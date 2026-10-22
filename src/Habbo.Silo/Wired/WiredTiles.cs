using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>The tile sets the area and neighbourhood selectors draw.</summary>
public static class WiredTiles
{
    /// <summary>How far the neighbourhood drawing reaches from its centre (the client's
    /// NeighborhoodFloor.RADIUS).</summary>
    public const int NeighborhoodRadius = 10;

    /// <summary>A new neighbourhood box on Habbo: the 5 by 5 tiles around its origin, the spiral's first 25 ranks, in a
    /// mask of 15 words.</summary>
    public static IReadOnlyList<int> DefaultNeighborhood => [1, 0, 0, (1 << 25) - 1, .. new int[14]];

    /// <summary>The tiles of an area: from its corner, so many wide and so many high.</summary>
    public static bool InArea(WiredBox box, int x, int y) =>
        x >= box.IntParam(0) && x < box.IntParam(0) + box.IntParam(2)
        && y >= box.IntParam(1) && y < box.IntParam(1) + box.IntParam(3);

    /// <summary>The offsets a neighbourhood drawing marks, from where its reference tile is. The
    /// drawing comes as a bit per tile, the tiles in the order of a spiral out from the centre,
    /// packed little-endian into the ints after the reference tile's x and y.</summary>
    public static IReadOnlyList<(int X, int Y)> Neighborhood(WiredBox box)
    {
        var (rootX, rootY) = (box.IntParam(1), box.IntParam(2));
        var bits = box.IntParams.Skip(3).SelectMany(BitConverter.GetBytes).ToArray();
        var offsets = new List<(int X, int Y)>();
        var rank = 0;
        foreach (var (x, y) in Spiral(NeighborhoodRadius))
        {
            if (rank / 8 < bits.Length && (bits[rank / 8] & (1 << (rank % 8))) != 0)
                offsets.Add((x - rootX, y - rootY));
            rank++;
        }

        return offsets;
    }

    /// <summary>The tiles around the given ones that a neighbourhood's offsets reach.</summary>
    public static HashSet<(int X, int Y)> Around(WiredBox box, IEnumerable<(int X, int Y)> origins)
    {
        var offsets = Neighborhood(box);
        return origins.SelectMany(origin => offsets.Select(offset => (origin.X + offset.X, origin.Y + offset.Y))).ToHashSet();
    }

    /// <summary>The client's spiral walk (SpiralUtils.walkSpiral): from the centre, east one, north
    /// one, west two, south two, east three, and so on, over a square of the radius each way.</summary>
    private static IEnumerable<(int X, int Y)> Spiral(int radius)
    {
        var side = radius * 2 + 1;
        var total = side * side;
        var (x, y, dx, dy, emitted) = (0, 0, 1, 0, 0);
        for (var length = 1; length <= side; length++)
        {
            for (var leg = 0; leg < 2; leg++)
            {
                for (var step = 0; step < length; step++)
                {
                    yield return (x, y);
                    (x, y) = (x + dx, y + dy);
                    if (++emitted == total)
                        yield break;
                }

                (dx, dy) = (dx, dy) switch
                {
                    (0, -1) => (-1, 0),
                    (1, 0) => (0, -1),
                    (0, 1) => (1, 0),
                    _ => (0, 1),
                };
            }
        }
    }

    /// <summary>The origins a neighbourhood is drawn around: its users' tiles, or its furni's.</summary>
    public static IEnumerable<(int X, int Y)> Origins(WiredBox box, WiredRun run) =>
        box.Flag(0)
            ? run.Users(box).Select(user => (user.X, user.Y))
            : run.Furni(box).SelectMany(RoomItemStore.Footprint);
}
