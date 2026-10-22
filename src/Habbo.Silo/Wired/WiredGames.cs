using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>What the team and clock boxes share: which team a box means, clock times in the
/// dialogs' shape, and comparisons.</summary>
public static class WiredGames
{
    /// <summary>"Any team" (for the in-team boxes) and "triggerer's team" (for the score boxes).</summary>
    public const int AnyOrTriggererTeam = 0;

    /// <summary>A clock time from a dialog's seconds, minutes and half-second, in pulses.</summary>
    public static int Pulses(int seconds, int minutes, int half) => (minutes * 60 + seconds) * 2 + half;

    // comparison.0/1/2
    public const int LowerThan = 0;
    public const int EqualTo = 1;
    public const int HigherThan = 2;

    public static bool Compare(int value, int comparison, int with) => comparison switch
    {
        LowerThan => value < with,
        HigherThan => value > with,
        _ => value == with,
    };

    /// <summary>The picks of a clock box: counters only.</summary>
    public static readonly WiredPickRule Clocks = new(RoomGames.IsClock, "wiredfurni.error.require_counter_furni");
}
