using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Protocol.Messages.Outgoing.RoomSettings;
using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The staff's commands about this room and the people in it: words put in someone's mouth, mutes, the room
/// unloaded or reloaded, and going unseen.</summary>
public sealed partial class RoomGrain
{
    /// <summary>The user in the room by that name, telling the asker when there is none.</summary>
    private RoomEntityStore.RoomEntity? UserHere(RoomEntityStore.RoomEntity asker, string name)
    {
        var found = _entities!.Entities.FirstOrDefault(e => e.Bot is null && e.Pet is null
            && string.Equals(e.Player.Username, name, StringComparison.OrdinalIgnoreCase));
        if (found is null)
            TellPlayer(asker, $"Nobody here is called {name}.");
        return found;
    }

    /// <summary>:makesay &lt;username&gt; &lt;message&gt; — makes a player in the room say something.</summary>
    private async Task MakeSayAsync(RoomEntityStore.RoomEntity staff, string argument)
    {
        var parts = argument.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            TellPlayer(staff, ":makesay <username> <message>");
            return;
        }

        if (UserHere(staff, parts[0]) is not { } speaker || !Outranks(staff, speaker.Player.Username, speaker.Player.Rank))
            return;

        var message = new ChatMessage(speaker.RoomIndex, parts[1], await gestures.DetectAsync(parts[1]), 0, Links: [], TrackingId: -1);
        SendChat(message, RoomChatKind.Talk, WithinHearing(speaker), toSpectators: true);
    }

    /// <summary>:mute &lt;username&gt; [minutes] / :unmute &lt;username&gt; — mutes a player hotel wide, or lets them
    /// talk again; as the mod tool's mute does.</summary>
    private async Task MuteAsync(RoomEntityStore.RoomEntity staff, string argument, bool mute)
    {
        var parts = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var minutes = 0;
        if (parts.Length == 0 || (mute && parts.Length > 1 && (!int.TryParse(parts[1], out minutes) || minutes < 1)))
        {
            TellPlayer(staff, mute ? ":mute <username> [minutes]" : ":unmute <username>");
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var name = parts[0].ToLowerInvariant();
        if (await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == name) is not { } user)
        {
            TellPlayer(staff, $"No player is called {parts[0]}.");
            return;
        }

        if (!Outranks(staff, user.Username, user.Rank))
            return;

        user.MutedUntil = mute ? DateTimeOffset.UtcNow.AddMinutes(minutes > 0 ? minutes : roomOptions.Value.CommandMuteMinutes) : null;
        await db.SaveChangesAsync();

        // the player's own view of it, and the room they are in
        var player = GrainFactory.GetGrain<IPlayerGrain>(user.Id);
        var data = await player.ReloadAsync();
        if (await player.GetCurrentRoomAsync() is { } roomId)
            await GrainFactory.GetGrain<IGameRoomGrain>(roomId).RefreshPlayerAsync(user.Id, data);
        TellPlayer(staff, mute ? $"{user.Username} is muted until {user.MutedUntil:yyyy-MM-dd HH:mm} (UTC)." : $"{user.Username} may talk again.");
    }

    /// <summary>:roommute — mutes everyone in the room without rights, or lets them talk again.</summary>
    private async Task RoomMuteAsync(RoomEntityStore.RoomEntity staff, string argument)
    {
        _room!.AllInRoomMuted = !_room.AllInRoomMuted;
        await SaveRoomAsync();
        Broadcast(new MuteAllInRoomComposer(_room.AllInRoomMuted));
        TellPlayer(staff, _room.AllInRoomMuted ? "The room is muted." : "The room may talk again.");
    }

    /// <summary>:unload — sends everyone out and lets the room unload.</summary>
    private async Task UnloadAsync(RoomEntityStore.RoomEntity staff, string argument)
    {
        foreach (var entity in _entities!.Entities.Where(e => e.Bot is null && e.Pet is null).ToList())
        {
            SendTo(entity.Player.Id, new CloseConnectionMessageComposer());
            await LeaveAsync(entity.Player.Id);
        }

        DeactivateOnIdle();
    }

    /// <summary>:reloadroom — loads the room afresh, everyone brought back in.</summary>
    private async Task ReloadRoomAsync(RoomEntityStore.RoomEntity staff, string argument)
    {
        foreach (var entity in _entities!.Entities.Where(e => e.Bot is null && e.Pet is null).ToList())
        {
            _summoned.Give(entity.Player.Id);
            SendTo(entity.Player.Id, new RoomForwardMessageComposer(_room!.Id));
            await LeaveAsync(entity.Player.Id);
        }

        DeactivateOnIdle();
    }

    /// <summary>:invisible — unseen in the rooms one enters from now on, here too, or seen again.</summary>
    private Task ToggleInvisibleAsync(RoomEntityStore.RoomEntity staff, string argument)
    {
        staff.Invisible = invisiblePlayers.Toggle(staff.Player.Id);
        if (staff.Invisible)
            BroadcastExcept(staff.Player.Id, new UserRemoveMessageComposer(staff.RoomIndex));
        else
            BroadcastExcept(staff.Player.Id, UsersMessageComposer.From([_entities!.ToWireData(staff)]));
        TellPlayer(staff, staff.Invisible ? "You are invisible." : "You are visible again.");
        return Task.CompletedTask;
    }
}
