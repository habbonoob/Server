using Habbo.Silo.Rooms;

namespace Habbo.Silo.Tests;

/// <summary>The mask is read bit by bit by the client, so the order the bits come out in is the
/// whole contract. These decode it back the way FurnitureWaterAreaVisualization does.</summary>
public class WaterAreaBorderMaskTests
{
    private static int Mask(
        int x, int y, int width, int height, Func<int, int, bool> isRoomTile, Func<int, int, bool>? isWater = null) =>
        WaterAreaBorderMask.For(x, y, width, height, isRoomTile, isWater ?? ((_, _) => false));

    [Fact]
    public void SetsNoBitsWhenEveryNeighbourIsFloor()
    {
        Assert.Equal(0, Mask(5, 5, 2, 2, (_, _) => true));
    }

    [Fact]
    public void MarksTheRingWhereTheRoomEnds()
    {
        // A 2x2 water area at (0,0) of a room that is only floor from (0,0) to (1,1): every ring
        // tile is off the floor, so only the four corners stay clear.
        var mask = Mask(0, 0, 2, 2, (x, y) => x is >= 0 and <= 1 && y is >= 0 and <= 1);

        Assert.Equal(0b0110_1111_0110, mask);
    }

    [Fact]
    public void MarksTheSideAnotherWaterAreaSharesSoNoShoreIsDrawnBetweenThem()
    {
        // The neighbour covers x = 2, the two tiles right of a 2x2 area at (0,0).
        var mask = Mask(0, 0, 2, 2, (_, _) => true, (x, y) => x == 2 && y is 0 or 1);

        // The right-edge bits are the first of each middle row: bits 4 and 6.
        Assert.Equal((1 << 4) | (1 << 6), mask);
    }

    [Fact]
    public void ReadsBackThroughTheClientsOwnDecodingOfTheRing()
    {
        var width = 3;
        var height = 2;
        bool IsRoomTile(int x, int y) => y >= 0;

        var mask = Mask(4, 0, width, height, IsRoomTile);
        var grid = Decode(mask, width, height);

        // The row above the item is off the floor, and nothing else is.
        Assert.Equal([false, true, true, true, false], grid[0]);
        Assert.Equal([false, false, false, false, false], grid[height + 1]);
        for (var row = 1; row <= height; row++)
            Assert.Equal([false, false, false, false, false], grid[row]);
    }

    /// <summary>The client's own reading: the row below the item right to left, then each of its
    /// rows bottom to top taking the right edge before the left, then the row above it.</summary>
    private static bool[][] Decode(int mask, int width, int height)
    {
        var grid = new bool[height + 2][];
        for (var row = 0; row < grid.Length; row++)
            grid[row] = new bool[width + 2];

        for (var column = width + 1; column >= 0; column--)
        {
            grid[height + 1][column] = (mask & 1) == 1;
            mask >>= 1;
        }

        for (var row = height; row >= 1; row--)
        {
            grid[row][width + 1] = (mask & 1) == 1;
            mask >>= 1;
            grid[row][0] = (mask & 1) == 1;
            mask >>= 1;
        }

        for (var column = width + 1; column >= 0; column--)
        {
            grid[0][column] = (mask & 1) == 1;
            mask >>= 1;
        }

        return grid;
    }
}
