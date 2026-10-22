using Habbo.Protocol.Rooms;

namespace Habbo.Silo.Rooms;

/// <summary>Habbo's pathfinding, a reverse dijkstra: the search starts at the goal and spreads backwards, straights
/// before diagonals, and every tile it reaches remembers the tile it was reached from, its next step towards the goal
/// (that step must be walkable, so slopes count the way they are walked). The walker then just follows those steps from
/// where it stands. Because the tiles around the goal hang off it in straight lines, the walk goes diagonally first
/// and straight at the end, as on Habbo. Every step costs the same, diagonals included.</summary>
public static class Pathfinder
{
    private static readonly (int Dx, int Dy)[] Moves =
    [
        (0, -1), (-1, 0), (1, 0), (0, 1),   // straights first
        (-1, -1), (1, -1), (-1, 1), (1, 1),
    ];

    /// <summary>The straight moves: the first of <see cref="Moves"/>, all a room without diagonals allows.</summary>
    private const int StraightMoves = 4;

    /// <summary>Finds a walkable route from <paramref name="start"/> to <paramref name="target"/>,
    /// skipping every tile for which <paramref name="isOccupied"/> returns true, the target
    /// included: only the caller knows whether the target is somewhere the walk may end, since a
    /// chair blocks the way through and is still somewhere to walk to.
    /// Returns null when unreachable; an empty list when start equals target.</summary>
    public static List<(int X, int Y)>? FindPath(
        RoomModel model, (int X, int Y) start, (int X, int Y) target, Func<int, int, bool> isOccupied, bool diagonal = true)
    {
        if (!model.IsTile(start.X, start.Y) || !model.IsTile(target.X, target.Y))
            return null;

        if (start == target)
            return [];

        if (isOccupied(target.X, target.Y))
            return null;

        var moves = diagonal ? Moves : Moves[..StraightMoves];
        var towardGoal = StepsTowardGoal(model, start, target, isOccupied, moves);
        return towardGoal.ContainsKey(start) ? Follow(start, target, towardGoal) : null;
    }

    /// <summary>The route to the target, or — when it cannot be reached — the route to the
    /// reachable tile closest to it. Two avatars may walk for the same tile: nothing is reserved
    /// ahead of time, so whoever arrives first keeps it and the other settles for the nearest
    /// tile it can still get to. Returns null when the target is not a tile of this room, and a
    /// single-tile path when the start already is as close as it can get.</summary>
    public static List<(int X, int Y)>? FindPathToNearest(
        RoomModel model, (int X, int Y) start, (int X, int Y) target, Func<int, int, bool> isOccupied, bool diagonal = true)
    {
        if (!model.IsTile(start.X, start.Y) || !model.IsTile(target.X, target.Y))
            return null;

        if (start == target)
            return [];

        if (FindPath(model, start, target, isOccupied, diagonal) is { } path)
            return path;

        // Closest to the target first, then the shortest walk to it. Tiles equal on both are
        // equally good, so the coordinates settle it and the same click always walks the same way.
        var nearest = Reachable(model, start, isOccupied, diagonal ? Moves : Moves[..StraightMoves])
            .OrderBy(tile => Distance(tile.Key, target))
            .ThenBy(tile => tile.Value)
            .ThenBy(tile => tile.Key.X)
            .ThenBy(tile => tile.Key.Y)
            .First().Key;

        return nearest == start ? [start] : FindPath(model, start, nearest, isOccupied, diagonal);
    }

    /// <summary>The search backwards from the goal: every tile it reaches, with the tile it was reached from, which is
    /// its next step towards the goal. It stops once the start is reached.</summary>
    private static Dictionary<(int X, int Y), (int X, int Y)> StepsTowardGoal(
        RoomModel model, (int X, int Y) start, (int X, int Y) goal, Func<int, int, bool> isOccupied, (int Dx, int Dy)[] moves)
    {
        var towardGoal = new Dictionary<(int X, int Y), (int X, int Y)> { [goal] = goal };
        var frontier = new Queue<(int X, int Y)>();
        frontier.Enqueue(goal);

        while (frontier.TryDequeue(out var current))
        {
            foreach (var (dx, dy) in moves)
            {
                var from = (X: current.X + dx, Y: current.Y + dy);
                if (towardGoal.ContainsKey(from) || !model.IsTile(from.X, from.Y))
                    continue;

                // the walker would step from there onto this tile, so that is the step that must be walkable; the
                // walker's own tile is never in its way
                if (!model.IsWalkableStep(from.X, from.Y, current.X, current.Y))
                    continue;

                if (from != start && isOccupied(from.X, from.Y))
                    continue;

                towardGoal[from] = current;
                if (from == start)
                    return towardGoal;
                frontier.Enqueue(from);
            }
        }

        return towardGoal;
    }

    /// <summary>The walk from the start to the goal: each tile's next step, in turn.</summary>
    private static List<(int X, int Y)> Follow(
        (int X, int Y) start, (int X, int Y) goal, Dictionary<(int X, int Y), (int X, int Y)> towardGoal)
    {
        var path = new List<(int X, int Y)> { start };
        for (var current = start; current != goal; current = towardGoal[current])
            path.Add(towardGoal[current]);
        return path;
    }

    /// <summary>Every tile the start can walk to, with its steps from the start: what the nearest tile to an
    /// unreachable target is chosen from.</summary>
    private static Dictionary<(int X, int Y), int> Reachable(
        RoomModel model, (int X, int Y) start, Func<int, int, bool> isOccupied, (int Dx, int Dy)[] moves)
    {
        var reached = new Dictionary<(int X, int Y), int> { [start] = 0 };
        var frontier = new Queue<(int X, int Y)>();
        frontier.Enqueue(start);

        while (frontier.TryDequeue(out var current))
        {
            foreach (var (dx, dy) in moves)
            {
                var next = (X: current.X + dx, Y: current.Y + dy);
                if (reached.ContainsKey(next) || !model.IsWalkableStep(current.X, current.Y, next.X, next.Y)
                    || isOccupied(next.X, next.Y))
                    continue;

                reached[next] = reached[current] + 1;
                frontier.Enqueue(next);
            }
        }

        return reached;
    }

    /// <summary>How far apart two tiles are in steps, diagonals counting as one.</summary>
    private static int Distance((int X, int Y) from, (int X, int Y) to) =>
        Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y));
}
