namespace Habbo.Silo.Rooms;

/// <summary>The border bitmask a water area carries as its state. The client rebuilds a
/// (width + 2) x (height + 2) grid around the item, marks the item's own tiles as water and
/// then reads this mask bit by bit to learn which surrounding tiles are water too; it draws a
/// shore edge along every ring tile that is not. So the mask says where no shore belongs:
/// against the void outside the room, and against a neighbouring water area the two share.
/// </summary>
public static class WaterAreaBorderMask
{
    /// <summary>The mask for an item covering <paramref name="width"/> x <paramref name="height"/>
    /// tiles at (<paramref name="x"/>, <paramref name="y"/>). <paramref name="isWater"/> answers
    /// whether a tile is another water area, <paramref name="isRoomTile"/> whether it is floor.
    /// Bits come out in the order the client consumes them, least significant first: the row
    /// below the item right to left, then each row of the item (right edge, then left edge)
    /// bottom to top, then the row above it right to left.</summary>
    public static int For(
        int x, int y, int width, int height, Func<int, int, bool> isRoomTile, Func<int, int, bool> isWater)
    {
        var bit = 0;
        var mask = 0;

        void Take(int column, int row)
        {
            // The client reads the four corners to pick between a straight edge and a rounded
            // cap, and a corner is never part of the area, so it stays clear.
            var isCorner = (column == 0 || column == width + 1) && (row == 0 || row == height + 1);
            if (!isCorner && NoShore(x + column - 1, y + row - 1))
                mask |= 1 << bit;
            bit++;
        }

        bool NoShore(int tileX, int tileY) => !isRoomTile(tileX, tileY) || isWater(tileX, tileY);

        for (var column = width + 1; column >= 0; column--)
            Take(column, height + 1);

        for (var row = height; row >= 1; row--)
        {
            Take(width + 1, row);
            Take(0, row);
        }

        for (var column = width + 1; column >= 0; column--)
            Take(column, 0);

        return mask;
    }
}
