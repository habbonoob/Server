namespace Habbo.Silo.Rooms.Copies;

/// <summary>Which copied teleports go together. Habbo tells everyone in a room which teleport each teleport is linked
/// to, by its id there, in the floor item's extra number (<see cref="Habbo.Domain.Rooms.Copies.RoomCopyFloorItem.Extra"/>);
/// two teleports of the copy that name each other are a pair. One whose partner stood in another room, or that a copy
/// older than format 4 doesn't say, is in none.</summary>
public static class CopiedTeleports
{
    /// <summary>The pairs, each once, by the teleports' ids on the hotel they were copied from.</summary>
    public static IReadOnlyList<(long A, long B)> Pair(IEnumerable<(long SourceId, long Extra)> teleports)
    {
        var partners = new Dictionary<long, long>();
        foreach (var (sourceId, extra) in teleports)
        {
            if (sourceId != 0 && extra != 0 && extra != sourceId)
                partners.TryAdd(sourceId, extra);
        }

        return partners
            .Where(p => p.Key < p.Value && partners.GetValueOrDefault(p.Value) == p.Key)
            .Select(p => (p.Key, p.Value))
            .ToList();
    }
}
