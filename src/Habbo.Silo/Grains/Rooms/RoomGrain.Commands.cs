using Habbo.Domain.Users;
using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The commands the client sends as chat once the player has confirmed them: picking up all the furniture
/// they may, sending other people's furniture or pets home, clearing the high score boards, and the effects a word
/// puts on.</summary>
public sealed partial class RoomGrain
{
    private const char CommandPrefix = ':';
    private const string PickAllCommand = "pickall";
    private const string EjectAllCommand = "ejectall";
    private const string EjectPetsCommand = "ejectpets";
    private const string ResetScoresCommand = "resetscores";
    private const string GiveCommand = "give";
    private const string GiveToRoom = "room";
    private const string GiveToHotel = "hotel";

    /// <summary>Does the command the text is, if it is one; false when it is only something said.</summary>
    private async Task<bool> RoomCommandAsync(RoomEntityStore.RoomEntity entity, string text)
    {
        if (text.Length < 2 || text[0] != CommandPrefix)
            return false;

        var space = text.IndexOf(' ');
        var command = (space < 0 ? text[1..] : text[1..space]).ToLowerInvariant();
        var argument = space < 0 ? string.Empty : text[(space + 1)..].Trim();
        if (await RunCommandAsync(entity, command, argument))
            return true;

        if (!roomOptions.Value.ChatEffects.TryGetValue(command, out var effect))
            return false;

        await ToggleChatEffectAsync(entity, effect);
        return true;
    }

    public Task<bool> IsAsleepAsync(Guid playerId) => Task.FromResult(_entities?.Find(playerId) is { Asleep: true });

    /// <summary>Staff give (or with a negative amount take) a currency: ":give &lt;currency&gt; &lt;amount&gt;
    /// &lt;username | room | hotel&gt;", the currency by the hotel's name for it (credits, duckets, diamonds, stars) or its
    /// number. The giver is told how it went.</summary>
    private async Task GiveCurrencyAsync(RoomEntityStore.RoomEntity giver, string argument)
    {
        var parts = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || !int.TryParse(parts[1], out var amount) || amount == 0
            || await currencies.ResolveAsync(parts[0]) is not { } type)
        {
            TellPlayer(giver, $"{CommandPrefix}{GiveCommand} <currency> <amount> <username, room or hotel>");
            return;
        }

        List<Guid> receivers;
        switch (parts[2].ToLowerInvariant())
        {
            case GiveToRoom:
                receivers = _entities!.Entities.Where(e => e.Bot is null && e.Pet is null).Select(e => e.Player.Id).Distinct().ToList();
                break;
            case GiveToHotel:
                receivers = await GrainFactory.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).GetOnlineAsync();
                break;
            default:
                await using (var db = await dbFactory.CreateDbContextAsync())
                {
                    var name = parts[2].ToLowerInvariant();
                    receivers = await db.Users.Where(u => u.Username.ToLower() == name).Select(u => u.Id).ToListAsync();
                }

                break;
        }

        foreach (var receiver in receivers)
            await currencies.GiveAsync(receiver, type, amount, $"given by {giver.Player.Username}");
        TellPlayer(giver, $"{amount} {parts[0]} to {receivers.Count} player(s)");
    }

    /// <summary>A notice to one player in the room: a line is whispered to them in the hotel's own bubble, and only a
    /// list of lines gets a window of its own.</summary>
    private void TellPlayer(RoomEntityStore.RoomEntity player, string text)
    {
        if (text.Contains('\n'))
        {
            SendTo(player.Player.Id, new Habbo.Protocol.Messages.Outgoing.Notifications.MOTDNotificationComposer([text]));
            return;
        }

        SendTo(player.Player.Id, new Habbo.Protocol.Messages.Outgoing.Room.Chat.WhisperMessageComposer(
            player.RoomIndex, text, Gesture: 0, roomOptions.Value.CommandReplyChatStyle, Links: [],
            TrackingId: Habbo.Protocol.Messages.Outgoing.Room.Chat.ChatMessageComposer.Untracked, ReceiverRoomIndex: player.RoomIndex,
            ChatBubbleWidthOverride: Habbo.Protocol.Messages.Outgoing.Room.Chat.ChatMessageComposer.RoomBubbleWidth));
    }

    /// <summary>Everything the player may pick up, back to whoever owns each item.</summary>
    private Task PickAllAsync(RoomEntityStore.RoomEntity picker) =>
        ReturnToOwnersAsync(_items!.Items.Where(item => MayPickUp(picker, item)).ToList(), picker.Player.AccountId);

    /// <summary>The owner (or staff) sends home everyone else's furniture, or only the named player's.</summary>
    private Task EjectAllAsync(RoomEntityStore.RoomEntity ejector, string username)
    {
        if (!IsOwnerOrStaff(ejector.Player.Id))
            return Task.CompletedTask;

        var theirs = _items!.Items.Where(item => item.Item.OwnerId != _room!.OwnerId
            && (username.Length == 0 || string.Equals(item.Item.Owner?.Username, username, StringComparison.OrdinalIgnoreCase)));
        return ReturnToOwnersAsync(theirs.ToList(), ejector.Player.AccountId);
    }

    /// <summary>The owner (or staff) sends home everyone else's pets.</summary>
    private async Task EjectPetsAsync(RoomEntityStore.RoomEntity ejector)
    {
        if (!IsOwnerOrStaff(ejector.Player.Id))
            return;

        // pets are kept apart from the users, in the store's own list
        foreach (var pet in _entities!.Pets.Where(e => e.Pet is { } p && p.Data.OwnerId != _room!.OwnerId).ToList())
            await ReturnPetAsync(pet);
    }

    /// <summary>The owner (or staff) clears every high score board in the room.</summary>
    private async Task ResetScoresAsync(RoomEntityStore.RoomEntity player)
    {
        if (!IsOwnerOrStaff(player.Player.Id) || _furniture is null)
            return;

        var boards = _items!.Items.Where(item => item.Interaction is Habbo.Silo.Items.Interactions.HighScoreInteraction
                                                 && item.Data.HighScoreEntries is { Count: > 0 });
        await _furniture.BatchedAsync(async () =>
        {
            foreach (var board in boards.ToList())
                await _furniture.UpdateAsync(board, board.Data with { HighScoreEntries = [] });
        });
    }

    /// <summary>Puts the effect on, or takes it off when it is already on; club only effects need the club.</summary>
    private async Task ToggleChatEffectAsync(RoomEntityStore.RoomEntity entity, RoomChatEffect effect)
    {
        if (effect.ClubOnly && entity.Player.ClubLevel == ClubLevel.None)
            return;
        if (await avatarEffects.FindAsync(effect.Library) is not { } effectId)
            return;

        ApplyEffect(entity, entity.EffectId == effectId ? 0 : effectId);
    }
}
