using Habbo.Protocol.Rooms;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Tests;

public class PathfinderTests
{
    private static readonly RoomModel Model = RoomModelParser.Parse(
        "xxxxxxxxxxxx" + "\r" +
        "xxx000000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxx00000000" + "\r" +
        "xxxxxxxxxxxx",
        doorX: 3, doorY: 1);

    private static readonly Func<int, int, bool> NothingOccupied = (_, _) => false;

    [Fact]
    public void StraightPathAcrossTheRoom()
    {
        var path = Pathfinder.FindPath(Model, (4, 1), (8, 1), NothingOccupied);

        Assert.NotNull(path);
        Assert.Equal((4, 1), path![0]);
        Assert.Equal((8, 1), path[^1]);
        Assert.Equal(5, path.Count); // 4 steps + start
    }

    [Theory]
    [InlineData(4, 4, 8, 6)]
    [InlineData(8, 6, 4, 4)]
    [InlineData(4, 6, 9, 2)]
    [InlineData(9, 2, 4, 6)]
    public void TheWalkGoesDiagonallyFirstThenStraight(int fromX, int fromY, int toX, int toY)
    {
        // as on Habbo: the search starts at the goal, so the tiles around it hang off it in straight lines
        var path = Pathfinder.FindPath(Model, (fromX, fromY), (toX, toY), NothingOccupied)!;

        var diagonal = path.Zip(path.Skip(1), (a, b) => a.X != b.X && a.Y != b.Y).ToList();
        var diagonals = Math.Min(Math.Abs(toX - fromX), Math.Abs(toY - fromY));
        Assert.Equal(Math.Max(Math.Abs(toX - fromX), Math.Abs(toY - fromY)), diagonal.Count);
        Assert.All(diagonal.Take(diagonals), Assert.True);
        Assert.All(diagonal.Skip(diagonals), Assert.False);
    }

    [Fact]
    public void AnUnreachableTargetWalksToTheNearestTileInstead()
    {
        var wall = new HashSet<(int, int)> { (7, 1), (7, 2), (7, 3), (7, 4), (7, 5), (7, 6) };

        var path = Pathfinder.FindPathToNearest(Model, (4, 4), (9, 4), (x, y) => wall.Contains((x, y)));

        // as close as the wall lets it (3 from the target), two steps from where it stood
        Assert.Equal(6, path![^1].X);
        Assert.Equal(3, path.Count);
    }

    [Fact]
    public void RefusesATargetTheCallerCallsOccupied()
    {
        // Whether a blocked target is still somewhere to walk to is the caller's call — a chair
        // is, a minibar is not — so the pathfinder asks about the target like any other tile.
        Assert.Null(Pathfinder.FindPath(Model, (4, 1), (8, 1), (x, y) => (x, y) == (8, 1)));
    }

    [Fact]
    public void DetoursAroundAnOccupiedTile()
    {
        var blocked = new HashSet<(int, int)> { (6, 1), (6, 2), (6, 3) };

        var path = Pathfinder.FindPath(Model, (4, 1), (8, 1), (x, y) => blocked.Contains((x, y)));

        Assert.NotNull(path);
        Assert.DoesNotContain(path!, p => blocked.Contains(p));
        Assert.Equal((8, 1), path![^1]);
        // The detour must go around the wall; every step must be an adjacent walkable tile.
        for (var i = 1; i < path.Count; i++)
        {
            var (fromX, fromY) = path[i - 1];
            var (toX, toY) = path[i];
            Assert.InRange(Math.Abs(fromX - toX) + Math.Abs(fromY - toY), 1, 2);
            Assert.True(Model.IsWalkableStep(fromX, fromY, toX, toY));
        }
    }

    [Fact]
    public void UnreachableTargetReturnsNull()
    {
        // The void border is unwalkable.
        Assert.Null(Pathfinder.FindPath(Model, (4, 1), (0, 0), NothingOccupied));
    }

    [Fact]
    public void StartEqualsTargetReturnsEmpty()
    {
        Assert.Empty(Pathfinder.FindPath(Model, (4, 1), (4, 1), NothingOccupied)!);
    }

    [Fact]
    public void ClimbableAndSteepSlopes()
    {
        // 0,1,2,4: heights 0→1→2 walkable, 2→4 is a steep wall.
        var slopes = RoomModelParser.Parse("0124", doorX: 0, doorY: 0);

        Assert.NotNull(Pathfinder.FindPath(slopes, (0, 0), (2, 0), NothingOccupied));   // climb 0→1→2
        Assert.Null(Pathfinder.FindPath(slopes, (0, 0), (3, 0), NothingOccupied));      // 2→4 is blocked
    }

    [Fact]
    public void DiagonalDoesNotCutCorners()
    {
        // The target is only adjacent diagonally, and both orthogonal neighbors are void —
        // the diagonal step is illegal, so no path exists.
        var corner = RoomModelParser.Parse("0x" + "\r" + "x0", doorX: 0, doorY: 0);

        Assert.Null(Pathfinder.FindPath(corner, (0, 0), (1, 1), NothingOccupied));
    }
}
