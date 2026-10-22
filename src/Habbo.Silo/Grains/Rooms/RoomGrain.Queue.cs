using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The line outside a full room, and the spectators watching it without an avatar. A room
/// has them only when its data gives it a queue length and spectator places: those waiting are let
/// in as places free up, club members first.</summary>
public sealed partial class RoomGrain
{
    /// <summary>The client's two lines, by the number it asks to change to.</summary>
    public const int SpectatorQueue = 1;
    public const int VisitorQueue = 2;

    // the client's names for a line's two queues: club members' and everyone else's
    private const string ClubLine = "c";
    private const string NormalLine = "d";

    private const string SpectatorQueueName = "spectators";
    private const string VisitorQueueName = "visitors";

    private readonly List<QueuedVisitor> _queue = [];

    /// <summary>Spectators, by player, with the session each watches through.</summary>
    private readonly Dictionary<Guid, Guid> _spectators = [];

    /// <summary>Who the queue let in, as what, and until when their place is kept for them. Leaving
    /// the line does not give it up: the client leaves the room it waited at on its way back in.</summary>
    private readonly Dictionary<Guid, (int Target, DateTimeOffset Until)> _letInFromQueue = [];

    /// <summary>Visitor places kept for those the queue let in, which nobody else may take.</summary>
    private int VisitorPlacesKept => _letInFromQueue.Values.Count(l => l.Target == VisitorQueue && l.Until > DateTimeOffset.UtcNow);

    private bool HasSpectatorPlaces => _room is { SpectatorLimit: > 0 };

    private bool VisitorPlaceFree => _entities is not null && _room is not null && _entities.Count < _room.MaxUsers;

    private bool SpectatorPlaceFree => _room is not null && _spectators.Count < _room.SpectatorLimit;

    /// <summary>A full room's answer to someone at the door: into the line if the room has one.</summary>
    private RoomAccess Queue(Guid playerId, Guid sessionGuid, PlayerData player)
    {
        if (_room is null || _queue.Count >= _room.QueueLimit)
            return RoomAccess.Full;

        _queue.RemoveAll(q => q.PlayerId == playerId);
        _queue.Add(new QueuedVisitor(playerId, sessionGuid, player, VisitorQueue));
        SendQueueStatus();
        return RoomAccess.Queued;
    }

    /// <summary>How the door answers someone the queue let in.</summary>
    private RoomAccess? LetInFromQueue(Guid playerId) =>
        _letInFromQueue.Remove(playerId, out var place) && place.Until > DateTimeOffset.UtcNow
            ? place.Target == SpectatorQueue ? RoomAccess.Spectator : RoomAccess.Allowed
            : null;

    public async Task ChangeQueueAsync(Guid playerId, PlayerData player, int target)
    {
        // a spectator may line up to come in properly, watching meanwhile
        if (_spectators.TryGetValue(playerId, out var watching) && target == VisitorQueue && _queue.All(q => q.PlayerId != playerId))
            _queue.Add(new QueuedVisitor(playerId, watching, player, VisitorQueue));

        var index = _queue.FindIndex(q => q.PlayerId == playerId);
        if (index < 0 || target is not (SpectatorQueue or VisitorQueue) || (target == SpectatorQueue && !HasSpectatorPlaces))
            return;

        // a change of line is a place at the back of the other
        var queued = _queue[index];
        _queue.RemoveAt(index);
        _queue.Add(queued with { Target = target });
        await LetInQueuedPlayersAsync();
        SendQueueStatus();
    }

    /// <summary>Lets in whoever is first in each line, while there is space.</summary>
    private async Task LetInQueuedPlayersAsync()
    {
        foreach (var (target, free) in new[] { (VisitorQueue, (Func<bool>)(() => VisitorPlaceFree)), (SpectatorQueue, () => SpectatorPlaceFree) })
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var lapsed in _letInFromQueue.Where(l => l.Value.Until <= now).Select(l => l.Key).ToList())
                _letInFromQueue.Remove(lapsed);
            var reserved = _letInFromQueue.Count(l => l.Value.Target == target);
            while (Ordered(target).FirstOrDefault() is { } next && free() && reserved == 0)
            {
                _queue.Remove(next);
                if (_spectators.Remove(next.PlayerId))
                {
                    // a spectator's avatar steps out of the stands, into the room
                    await StepOutOfStandsAsync(next);
                    continue;
                }

                _letInFromQueue[next.PlayerId] = (target, now.AddSeconds(roomOptions.Value.QueuePlaceKeptSeconds));
                reserved++;
                // the client comes back to the door, and is let in
                SendToSession(next.SessionGuid, new RoomForwardMessageComposer(_room!.Id));
            }
        }
    }

    /// <summary>A line in the order it moves: club members before everyone else.</summary>
    private IEnumerable<QueuedVisitor> Ordered(int target) =>
        _queue.Where(q => q.Target == target).OrderByDescending(q => q.IsClub);

    private void SendQueueStatus()
    {
        if (_room is null)
            return;

        foreach (var queued in _queue)
        {
            var sets = new List<RoomQueueSetData>();
            foreach (var (target, name) in QueueTargets())
            {
                var line = Ordered(target).ToList();
                var position = line.IndexOf(queued);
                var ahead = position >= 0 ? position : line.Count;
                var clubAhead = line.Take(ahead).Count(q => q.IsClub);
                sets.Add(new RoomQueueSetData(name, target, [
                    new RoomQueueData(ClubLine, queued.IsClub ? clubAhead : line.Count(q => q.IsClub)),
                    new RoomQueueData(NormalLine, ahead),
                ]));
            }

            // the line the player stands in comes first: the client takes it as the active one
            sets = sets.OrderByDescending(set => set.Target == queued.Target).ToList();
            SendToSession(queued.SessionGuid, new RoomQueueStatusMessageComposer(_room.Id, sets));
        }
    }

    private IEnumerable<(int Target, string Name)> QueueTargets()
    {
        yield return (VisitorQueue, VisitorQueueName);
        if (HasSpectatorPlaces)
            yield return (SpectatorQueue, SpectatorQueueName);
    }

    /// <summary>Takes the player out of the line or the stands; true when they were in either.</summary>
    private async Task<bool> LeaveQueueOrStandsAsync(Guid playerId)
    {
        var left = _queue.RemoveAll(q => q.PlayerId == playerId) > 0 | _spectators.Remove(playerId);
        if (left)
        {
            await LetInQueuedPlayersAsync();
            SendQueueStatus();
        }

        return left;
    }

    public async Task<RoomLoadData?> SpectateAsync(Guid playerId, Guid sessionGuid, PlayerData player)
    {
        if (_room?.Model is null || _model is null || _items is null || _entities is null)
            return null;

        _spectators[playerId] = sessionGuid;
        await ReportPopulationAsync();
        return LoadData(player, RoomEntityStore.NoRoomIndex, RoomControllerLevels.None, canRate: false);
    }

    /// <summary>Makes a spectator a visitor where they stand: the room gains their avatar and they
    /// leave spectator mode.</summary>
    private async Task StepOutOfStandsAsync(QueuedVisitor visitor)
    {
        SendToSession(visitor.SessionGuid, new YouAreNotSpectatorMessageComposer(_room!.Id));
        if (await EnterAsync(visitor.PlayerId, visitor.SessionGuid, visitor.Player) is not { } load)
            return;

        var own = load.Entities.Where(e => e.RoomIndex == load.OwnRoomIndex).ToList();
        SendTo(visitor.PlayerId, UsersMessageComposer.From(own));
        SendTo(visitor.PlayerId, UserUpdateMessageComposer.From(load.Statuses.Where(s => s.RoomIndex == load.OwnRoomIndex).ToList()));
        await EnteredAsync(visitor.PlayerId);
    }

    /// <summary>What a room sends everyone, it sends its spectators too.</summary>
    private IEnumerable<Guid> SpectatorSessions => _spectators.Values;
}
