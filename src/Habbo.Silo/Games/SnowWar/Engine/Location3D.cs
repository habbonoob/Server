namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>A point in game units: a tile is 3200 across, and height runs the same way.</summary>
internal sealed class Location3D(int x, int y, int z)
{
    public int X { get; private set; } = x;
    public int Y { get; private set; } = y;
    public int Z { get; private set; } = z;

    public void Change(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public void Change2D(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void ChangeTo(Location3D other) => Change(other.X, other.Y, other.Z);

    /// <summary>The client's distance: the sum of the three differences.</summary>
    public int DistanceTo(Location3D other) =>
        SnowWarMath.Abs(other.X - X) + SnowWarMath.Abs(other.Y - Y) + SnowWarMath.Abs(other.Z - Z);

    public bool SameAs(Location3D other) => X == other.X && Y == other.Y && Z == other.Z;

    public bool IsInDistance(Location3D other, int distance) => IsInDistance(X, Y, other.X, other.Y, distance);

    /// <summary>Whether two points on the ground are closer than the distance, by the client's reckoning.</summary>
    public static bool IsInDistance(int x1, int y1, int x2, int y2, int distance)
    {
        var dx = SnowWarMath.Abs(x2 - x1);
        var dy = SnowWarMath.Abs(y2 - y1);
        if (dy > distance || dx > distance)
            return false;
        return (long)dx * dx + (long)dy * dy < (long)distance * distance;
    }
}
