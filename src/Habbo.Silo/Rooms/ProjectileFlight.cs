namespace Habbo.Silo.Rooms;

/// <summary>A furni's flight as a wired projectile: from where to where, when it set off and for how long the client
/// draws it (the move's time and its overshoot), and what it ran into. Its position on the way is worked out from the
/// time, as the client draws it: in a straight line.</summary>
public sealed class ProjectileFlight(
    int fromX, int fromY, double fromZ, int toX, int toY, double toZ, DateTimeOffset startedAt, int durationMilliseconds)
{
    public int FromX { get; } = fromX;
    public int FromY { get; } = fromY;
    public double FromZ { get; } = fromZ;
    public int ToX { get; } = toX;
    public int ToY { get; } = toY;
    public double ToZ { get; } = toZ;
    public DateTimeOffset StartedAt { get; } = startedAt;
    public int DurationMilliseconds { get; } = Math.Max(0, durationMilliseconds);

    /// <summary>The tiles it crossed: the longer of its two sides, as a step may go diagonally.</summary>
    public int Tiles => Math.Max(Math.Abs(ToX - FromX), Math.Abs(ToY - FromY));

    public int UserCollisions { get; set; }
    public int FurniCollisions { get; set; }

    public bool IsTravelling(DateTimeOffset now) => now < StartedAt.AddMilliseconds(DurationMilliseconds);

    /// <summary>How far along it is, from 0 (setting off) to 1 (landed).</summary>
    private double Progress(DateTimeOffset now) =>
        DurationMilliseconds == 0 ? 1 : Math.Clamp((now - StartedAt).TotalMilliseconds / DurationMilliseconds, 0, 1);

    public (int X, int Y, double Z) PositionAt(DateTimeOffset now)
    {
        var t = Progress(now);
        return ((int)Math.Round(FromX + (ToX - FromX) * t), (int)Math.Round(FromY + (ToY - FromY) * t), FromZ + (ToZ - FromZ) * t);
    }
}
