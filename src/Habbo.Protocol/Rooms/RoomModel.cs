namespace Habbo.Protocol.Rooms;

/// <summary>A parsed room layout: tile grid with per-tile heights, as sent to the client.</summary>
public sealed class RoomModel
{
    private readonly double[,] _heights;
    private readonly bool[,] _tiles;

    public RoomModel(int sizeX, int sizeY, int doorX, int doorY, double[,] heights, bool[,] tiles)
    {
        SizeX = sizeX;
        SizeY = sizeY;
        DoorX = doorX;
        DoorY = doorY;
        _heights = heights;
        _tiles = tiles;
    }

    public int SizeX { get; }
    public int SizeY { get; }
    public int DoorX { get; }
    public int DoorY { get; }

    public double DoorHeight => GetHeight(DoorX, DoorY);

    public bool IsTile(int x, int y) => x >= 0 && y >= 0 && x < SizeX && y < SizeY && _tiles[x, y];

    public double GetHeight(int x, int y) => IsTile(x, y) ? _heights[x, y] : 0;

    /// <summary>The classic walkability rule: a neighbor is reachable when the height
    /// difference is at most one step (1.0), no diagonal squeezing through corners.</summary>
    public bool IsWalkableStep(int fromX, int fromY, int toX, int toY)
    {
        if (!IsTile(toX, toY))
            return false;

        var heightDiff = Math.Abs(GetHeight(toX, toY) - GetHeight(fromX, fromY));
        if (heightDiff > 1.0)
            return false;

        // No corner cutting: a diagonal move needs both adjacent orthogonal tiles.
        if (fromX != toX && fromY != toY)
        {
            return IsTile(fromX, toY) && IsTile(toX, fromY)
                && Math.Abs(GetHeight(fromX, toY) - GetHeight(fromX, fromY)) <= 1.0
                && Math.Abs(GetHeight(toX, fromY) - GetHeight(fromX, fromY)) <= 1.0;
        }

        return true;
    }
}
