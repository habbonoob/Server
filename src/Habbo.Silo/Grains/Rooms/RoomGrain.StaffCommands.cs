using Habbo.Protocol.Messages.Outgoing.Handshake;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Protocol.Composers;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The staff's commands about players and rooms: bringing players over and going to them, a look at an
/// account, stopping or disconnecting someone, an alert to the room, and the room's most visitors.</summary>
public sealed partial class RoomGrain
{
    /// <summary>Players staff summoned here: their next entry passes the door, the password and any ban.</summary>
    private readonly EntryPasses _summoned = new(EntryPassLifetime);

    public Task ExpectSummonedAsync(Guid playerId)
    {
        _summoned.Give(playerId);
        return Task.CompletedTask;
    }

    /// <summary>The player named, if there is one.</summary>
    private async Task<Habbo.Domain.Users.User?> FindUserAsync(string username)
    {
        if (username.Length == 0)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync();
        var name = username.ToLowerInvariant();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username.ToLower() == name);
    }

    /// <summary>The player named as the first word of the argument, telling the asker when there is none.</summary>
    private async Task<Habbo.Domain.Users.User?> TargetAsync(RoomEntityStore.RoomEntity asker, string argument, string usage)
    {
        var name = argument.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        if (await FindUserAsync(name) is { } user)
            return user;

        TellPlayer(asker, name.Length == 0 ? usage : $"No player is called {name}.");
        return null;
    }

    /// <summary>Whether staff may act on the player: only on those below their own rank, telling them otherwise.</summary>
    private bool Outranks(RoomEntityStore.RoomEntity staff, string username, Habbo.Domain.Users.SecurityLevel rank)
    {
        if (rank < staff.Player.Rank)
            return true;

        TellPlayer(staff, $"{username} is of your rank or above.");
        return false;
    }

    private async Task<bool> IsOnlineAsync(Guid playerId) =>
        (await GrainFactory.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).WhichAreOnlineAsync([playerId])).Count > 0;

    /// <summary>Sends a player's client into a room.</summary>
    private Task ForwardAsync(Guid playerId, int roomId) =>
        GrainFactory.GetGrain<IPlayerGrain>(playerId).SendAsync(new RoomForwardMessageComposer(roomId).ToMessage());

    /// <summary>:summon &lt;username&gt; — brings a player online into this room, past its door.</summary>
    private async Task SummonAsync(RoomEntityStore.RoomEntity staff, string argument)
    {
        if (await TargetAsync(staff, argument, ":summon <username>") is not { } user || !Outranks(staff, user.Username, user.Rank))
            return;
        if (!await IsOnlineAsync(user.Id))
        {
            TellPlayer(staff, $"{user.Username} is not online.");
            return;
        }

        _summoned.Give(user.Id);
        await ForwardAsync(user.Id, _room!.Id);
        TellPlayer(staff, $"{user.Username} is on the way.");
    }

    /// <summary>:gotoroom &lt;room id&gt; — goes to a room.</summary>
    private async Task GoToRoomAsync(RoomEntityStore.RoomEntity staff, string argument)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!int.TryParse(argument.Trim(), out var roomId) || !await db.Rooms.AnyAsync(r => r.Id == roomId))
        {
            TellPlayer(staff, ":gotoroom <room id>");
            return;
        }

        await ForwardAsync(staff.Player.Id, roomId);
    }

    /// <summary>:follow &lt;username&gt; — goes to the room a player is in, past its door.</summary>
    private async Task FollowAsync(RoomEntityStore.RoomEntity staff, string argument)
    {
        if (await TargetAsync(staff, argument, ":follow <username>") is not { } user)
            return;
        if (await GrainFactory.GetGrain<IPlayerGrain>(user.Id).GetCurrentRoomAsync() is not { } roomId)
        {
            TellPlayer(staff, $"{user.Username} is not in a room.");
            return;
        }

        await GrainFactory.GetGrain<IGameRoomGrain>(roomId).ExpectSummonedAsync(staff.Player.Id);
        await ForwardAsync(staff.Player.Id, roomId);
    }

    /// <summary>:teleport — walking takes one straight to the tile clicked, or walks again.</summary>
    private Task ToggleTeleportAsync(RoomEntityStore.RoomEntity staff, string argument)
    {
        staff.Teleporting = !staff.Teleporting;
        TellPlayer(staff, staff.Teleporting ? "Teleporting: you go straight where you click." : "Walking again.");
        return Task.CompletedTask;
    }

    /// <summary>:userinfo &lt;username&gt; — a player's account at a glance.</summary>
    private async Task UserInfoAsync(RoomEntityStore.RoomEntity staff, string argument)
    {
        if (await TargetAsync(staff, argument, ":userinfo <username>") is not { } user)
            return;

        var online = await IsOnlineAsync(user.Id);
        var roomId = online ? await GrainFactory.GetGrain<IPlayerGrain>(user.Id).GetCurrentRoomAsync() : null;
        TellPlayer(staff, string.Join("\n",
            $"{user.Username} (account {user.AccountId})",
            $"Rank: {user.Rank}, club: {user.ClubLevel}",
            $"Motto: {user.Motto}",
            $"Credits: {user.Credits}",
            $"Joined: {user.CreatedAt:yyyy-MM-dd}, last login: {user.LastLoginAt:yyyy-MM-dd HH:mm}",
            online ? roomId is { } room ? $"Online, in room {room}" : "Online, in no room" : "Offline",
            user.MutedUntil is { } muted && muted > DateTimeOffset.UtcNow ? $"Muted until {muted:yyyy-MM-dd HH:mm}" : "Not muted"));
    }

    /// <summary>:freeze / :unfreeze &lt;username&gt; — stops a player in the room walking, or lets them again.</summary>
    private Task FreezeAsync(RoomEntityStore.RoomEntity staff, string argument, bool frozen)
    {
        var name = argument.Trim();
        if (_entities!.Entities.FirstOrDefault(e => e.Bot is null && e.Pet is null
                && string.Equals(e.Player.Username, name, StringComparison.OrdinalIgnoreCase)) is not { } target)
        {
            TellPlayer(staff, $"Nobody here is called {name}.");
            return Task.CompletedTask;
        }

        if (!Outranks(staff, target.Player.Username, target.Player.Rank))
            return Task.CompletedTask;

        target.Frozen = frozen;
        if (frozen)
        {
            target.Path.Clear();
            target.Goal = null;
        }

        TellPlayer(staff, frozen ? $"{target.Player.Username} is frozen." : $"{target.Player.Username} may walk again.");
        return Task.CompletedTask;
    }

    /// <summary>:disconnect &lt;username&gt; — disconnects a player from the hotel.</summary>
    private async Task DisconnectPlayerAsync(RoomEntityStore.RoomEntity staff, string argument)
    {
        if (await TargetAsync(staff, argument, ":disconnect <username>") is not { } user)
            return;
        if (user.Rank >= staff.Player.Rank)
        {
            TellPlayer(staff, $"{user.Username} is of your rank or above.");
            return;
        }

        await GrainFactory.GetGrain<IPlayerGrain>(user.Id).DisconnectAsync(DisconnectReasonComposer.Logout);
        TellPlayer(staff, $"{user.Username} is disconnected.");
    }

    /// <summary>:roomalert &lt;message&gt; — an alert to everyone in the room.</summary>
    private Task RoomAlertAsync(RoomEntityStore.RoomEntity staff, string argument)
    {
        if (argument.Trim().Length == 0)
        {
            TellPlayer(staff, ":roomalert <message>");
            return Task.CompletedTask;
        }

        foreach (var entity in _entities!.Entities.Where(e => e.Bot is null && e.Pet is null).ToList())
            SendTo(entity.Player.Id, new Habbo.Protocol.Messages.Outgoing.Notifications.MOTDNotificationComposer([argument.Trim()]));
        return Task.CompletedTask;
    }

    /// <summary>:setmax &lt;amount&gt; — the room's most visitors, up to the hotel's limit (its owner, or staff).</summary>
    private async Task SetMaxAsync(RoomEntityStore.RoomEntity player, string argument)
    {
        if (!MayManage(player.Player))
            return;
        if (!int.TryParse(argument.Trim(), out var max) || max < 1 || max > roomOptions.Value.MaxVisitorsLimit)
        {
            TellPlayer(player, $":setmax <1-{roomOptions.Value.MaxVisitorsLimit}>");
            return;
        }

        _room!.MaxUsers = max;
        await SaveRoomAsync();
        TellPlayer(player, $"At most {max} visitors now.");
    }
}
