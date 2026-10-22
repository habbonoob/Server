using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>Wired and the room's bots: what the bot boxes have them do, and what they set off when they get where they
/// were going.</summary>
public sealed partial class RoomWired
{
    public bool WalkTo(RoomEntityStore.RoomEntity entity, int x, int y) => IsPresent(entity) && entities.Walk(entity, x, y);

    public Task BotSayAsync(RoomEntityStore.RoomEntity bot, string text, RoomChatKind kind, int bubbleWidth,
        IReadOnlyList<RoomEntityStore.RoomEntity>? listeners) =>
        IsPresent(bot) ? actions.Bots.Say(bot, text, kind, bubbleWidth, listeners) : Task.CompletedTask;

    public Task DressBotAsync(RoomEntityStore.RoomEntity bot, string figure) =>
        IsPresent(bot) ? actions.Bots.Dress(bot, figure) : Task.CompletedTask;

    public Task HandOverAsync(RoomEntityStore.RoomEntity? giver, RoomEntityStore.RoomEntity receiver, int handItem) =>
        IsPresent(receiver) && (giver is null || IsPresent(giver)) ? actions.Bots.HandOver(giver, receiver, handItem) : Task.CompletedTask;

    public Task BringAsync(RoomEntityStore.RoomEntity bot, RoomEntityStore.RoomEntity user, Func<Task> then) =>
        IsPresent(bot) && IsPresent(user) ? actions.Bots.WalkUpTo(bot, user, then) : Task.CompletedTask;

    public void Follow(RoomEntityStore.RoomEntity bot, RoomEntityStore.RoomEntity? user)
    {
        if (bot.Bot is not { } roomBot || !IsPresent(bot))
            return;

        roomBot.Following = user;
        roomBot.FollowedAt = null;
    }

    public void SetClickSettings(RoomEntityStore.RoomEntity user, int userOption, int furniOption)
    {
        if (user.Bot is null && user.Pet is null && IsPresent(user))
            sendTo(user.Player.Id, new Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredClickSettingsMessageComposer(userOption, furniOption));
    }

    public Task SendThroughAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem teleport) =>
        user.Bot is null && user.Pet is null && IsPresent(user) && items.Find(teleport.Item.Id) == teleport
            ? actions.SendThrough(user, teleport)
            : Task.CompletedTask;

    /// <summary>A bot ended a walk: the furni it stands on, and the users next to it, are reached.</summary>
    public async Task BotArrivedAsync(RoomEntityStore.RoomEntity bot)
    {
        if (_boxes.Count == 0 || bot.Bot is null)
            return;

        foreach (var item in items.FloorItemsOn(bot.X, bot.Y).ToList())
            await FireAsync(new WiredEvent(WiredEventType.BotReachedFurni, bot, item) { Bot = bot });

        foreach (var user in entities.Entities.Where(user => Math.Abs(user.X - bot.X) <= 1 && Math.Abs(user.Y - bot.Y) <= 1).ToList())
            await FireAsync(new WiredEvent(WiredEventType.BotReachedUser, user) { Bot = bot, ReachedUser = user });
    }
}
