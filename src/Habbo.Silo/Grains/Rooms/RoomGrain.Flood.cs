using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Flood control: say too much too quickly, by the room's sensitivity, and the room stops
/// carrying what you say for a while.</summary>
public sealed partial class RoomGrain
{
    private readonly Dictionary<Guid, Queue<DateTimeOffset>> _recentLines = [];
    private readonly Dictionary<Guid, DateTimeOffset> _floodedUntil = [];

    /// <summary>Forgets what a user who left said; a penalty still running stays, so leaving does not lift it, and
    /// penalties that have run out go.</summary>
    private void ForgetFlooding(Guid playerId)
    {
        _recentLines.Remove(playerId);
        var now = DateTimeOffset.UtcNow;
        foreach (var over in _floodedUntil.Where(f => f.Value <= now).Select(f => f.Key).ToList())
            _floodedUntil.Remove(over);
    }

    /// <summary>Whether the user is flooding (or still paying for it); they are told for how long.</summary>
    private bool IsFlooding(RoomEntityStore.RoomEntity entity)
    {
        var now = DateTimeOffset.UtcNow;
        var playerId = entity.Player.Id;
        if (_floodedUntil.TryGetValue(playerId, out var until) && until > now)
        {
            SendTo(playerId, new FloodControlMessageComposer((int)Math.Ceiling((until - now).TotalSeconds)));
            return true;
        }

        if (entity.ControllerLevel >= RoomControllerLevels.Staff
            || chatOptions.Value.FloodRules.FirstOrDefault(r => r.Sensitivity == _room!.ChatFloodSensitivity) is not { } rule)
            return false;

        var lines = _recentLines.TryGetValue(playerId, out var queue) ? queue : _recentLines[playerId] = new Queue<DateTimeOffset>();
        while (lines.Count > 0 && now - lines.Peek() > TimeSpan.FromSeconds(rule.WindowSeconds))
            lines.Dequeue();
        lines.Enqueue(now);
        if (lines.Count <= rule.MaxLines)
            return false;

        lines.Clear();
        _floodedUntil[playerId] = now.AddSeconds(rule.PenaltySeconds);
        SendTo(playerId, new FloodControlMessageComposer(rule.PenaltySeconds));
        return true;
    }
}
