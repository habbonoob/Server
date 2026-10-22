using Habbo.Silo.Abstractions.Permissions;
using Habbo.Domain.Groups;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;
using Habbo.Silo.Permissions;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Who may come in: bans, a full room, the password, and the doorbell somebody inside
/// has to answer.</summary>
public sealed partial class RoomGrain
{
    /// <summary>Who is ringing the doorbell, and the session to answer them on.</summary>
    private readonly Dictionary<Guid, (PlayerData Player, Guid SessionGuid)> _doorbell = [];

    /// <summary>How long a pass past the door lasts before it is used.</summary>
    private static readonly TimeSpan EntryPassLifetime = TimeSpan.FromMinutes(2);

    /// <summary>Who was let in at the door: their next entry passes it.</summary>
    private readonly EntryPasses _admitted = new(EntryPassLifetime);

    /// <summary>The name a doorbell answer carries for the one who rang.</summary>
    private const string ToTheVisitor = "";

    public async Task<RoomAccess> RequestAccessAsync(Guid playerId, Guid sessionGuid, PlayerData player, string password)
    {
        if (_room is null || _entities is null)
            return RoomAccess.NoSuchRoom;

        // staff and the room's own people come and go as they please
        var isStaff = permissions.Has(player, PermissionNames.RoomEnterAny);
        var inRoom = _entities.Find(playerId) is not null;
        if (isStaff || _room.OwnerId == playerId || inRoom)
            return RoomAccess.Allowed;
        if (_summoned.Take(playerId))
        {
            _waived.Give(playerId);
            return RoomAccess.Allowed;
        }

        if (LetInFromQueue(playerId) is { } fromQueue)
        {
            if (fromQueue == RoomAccess.Allowed)
                _waived.Give(playerId);
            return fromQueue;
        }

        if (await LockedByBuildersClubAsync())
        {
            await TellOwnerOfLockedVisitAsync(player.Username);
            return RoomAccess.LockedByBuildersClub;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.RoomBans.AnyAsync(b => b.RoomId == _room.Id && b.UserId == playerId && b.ExpiresAt > DateTimeOffset.UtcNow))
            return RoomAccess.Banned;

        // the door first, then the queue: one let into the queue is let into the room from it without the door
        var hasRights = _rights.Contains(playerId);
        var isMember = _group is not null && await db.GroupMembers.AnyAsync(m => m.GroupId == _group.Id && m.UserId == playerId);
        if (!hasRights && !isMember && !_admitted.Take(playerId))
        {
            switch (_room.State)
            {
                case RoomState.Password when password != _room.Password:
                    return RoomAccess.WrongPassword;
                case RoomState.NoobsOnly when !player.IsAmbassador && !IsNewPlayer(player):
                    return RoomAccess.NotANewPlayer;
                case RoomState.Locked:
                    return RingDoorbell(player, sessionGuid);
                // kept from listings and from those without rights alike: a link or a forward does not get them in
                case RoomState.Invisible:
                    return RoomAccess.NobodyToOpen;
            }
        }

        return _entities.Count + VisitorPlacesKept >= _room.MaxUsers && !hasRights
            ? Queue(playerId, sessionGuid, player)
            : RoomAccess.Allowed;
    }

    /// <summary>Who the door just let past any ban and a full room (summoned, or let in from the queue): the entry that
    /// follows is not held to them again.</summary>
    private readonly EntryPasses _waived = new(EntryPassLifetime);

    /// <summary>Whether an entry is turned away when it comes after all: banned, or the room filled up, since the door
    /// answered. Staff, the owner and those the door waived it for come in, as at the door.</summary>
    private async Task<bool> TurnedAwayAsync(Guid playerId, PlayerData player)
    {
        if (permissions.Has(player, PermissionNames.RoomEnterAny) || _room!.OwnerId == playerId || _waived.Take(playerId))
            return false;
        if (_entities!.Count + VisitorPlacesKept >= _room.MaxUsers && !_rights.Contains(playerId))
            return true;

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.RoomBans.AnyAsync(b => b.RoomId == _room.Id && b.UserId == playerId && b.ExpiresAt > DateTimeOffset.UtcNow);
    }

    /// <summary>A player whose account is still new enough for rooms kept for new players.</summary>
    private bool IsNewPlayer(PlayerData player) =>
        DateTimeOffset.UtcNow - player.CreatedAt < TimeSpan.FromDays(roomOptions.Value.NewPlayerDays);

    /// <summary>Rings for whoever inside may open: they see the name, the visitor waits. Unanswered,
    /// the visitor is told nobody came.</summary>
    private RoomAccess RingDoorbell(PlayerData player, Guid sessionGuid)
    {
        var openers = _entities!.Entities.Where(e => e.ControllerLevel >= RoomControllerLevels.Rights).ToList();
        if (openers.Count == 0)
            return RoomAccess.NobodyToOpen;

        _doorbell[player.Id] = (player, sessionGuid);
        foreach (var opener in openers)
            SendTo(opener.Player.Id, new DoorbellMessageComposer(player.Username));

        Schedule(TimeSpan.FromSeconds(roomOptions.Value.DoorbellTimeoutSeconds), () =>
        {
            if (_doorbell.Remove(player.Id, out var ringing))
                SendToSession(ringing.SessionGuid, new FlatAccessDeniedMessageComposer(_room!.Id, ToTheVisitor));
            return Task.CompletedTask;
        });
        return RoomAccess.Ringing;
    }

    public Task LetUserInAsync(Guid playerId, string userName, bool allow)
    {
        if (_room is null || _entities?.Find(playerId) is not { ControllerLevel: >= RoomControllerLevels.Rights })
            return Task.CompletedTask;

        var ringing = _doorbell.Values.FirstOrDefault(r => string.Equals(r.Player.Username, userName, StringComparison.OrdinalIgnoreCase));
        if (ringing.Player is null || !_doorbell.Remove(ringing.Player.Id))
            return Task.CompletedTask;

        var openers = _entities.Entities.Where(e => e.ControllerLevel >= RoomControllerLevels.Rights).ToList();
        if (allow)
        {
            // let in: the visitor's client is sent to the room again, and this time the door is open
            _admitted.Give(ringing.Player.Id);
            SendToSession(ringing.SessionGuid, new FlatAccessibleMessageComposer(_room.Id, ToTheVisitor));
            SendToSession(ringing.SessionGuid, new RoomForwardMessageComposer(_room.Id));
            foreach (var opener in openers)
                SendTo(opener.Player.Id, new FlatAccessibleMessageComposer(_room.Id, ringing.Player.Username));
        }
        else
        {
            SendToSession(ringing.SessionGuid, new FlatAccessDeniedMessageComposer(_room.Id, ToTheVisitor));
            foreach (var opener in openers)
                SendTo(opener.Player.Id, new FlatAccessDeniedMessageComposer(_room.Id, ringing.Player.Username));
        }

        return Task.CompletedTask;
    }

    /// <summary>Sends to a session that is not (yet) in the room.</summary>
    private void SendToSession(Guid sessionGuid, IComposer composer) =>
        sink.TrySend(sessionGuid, composer.ToMessage());
}
