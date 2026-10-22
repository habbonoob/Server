namespace Habbo.Silo.Wired;

/// <summary>How wired moves furni: how long the client animates it, what may stand in its way,
/// and who rides along. A stack's movement add-ons set these for its effects.</summary>
public sealed record WiredMoveOptions
{
    /// <summary>A wired move animates over one pulse unless an add-on says otherwise.</summary>
    public const int DefaultAnimationMilliseconds = 500;

    public static readonly WiredMoveOptions Default = new();

    public int AnimationMilliseconds { get; init; } = DefaultAnimationMilliseconds;

    /// <summary>Whether a user standing where the furni would go stops it.</summary>
    public bool AvatarsBlock { get; init; } = true;

    /// <summary>Whether any furni where it would go stops it, even one it could stand on.</summary>
    public bool BlockedByFurni { get; init; }

    /// <summary>Whether it ignores the furni where it goes: it may go onto what it could not stack on.</summary>
    public bool ThroughFurni { get; init; }

    /// <summary>Whether it keeps its altitude instead of settling on what is there.</summary>
    public bool KeepAltitude { get; init; }

    /// <summary>Who rides along (<see cref="Addons.CarryUsersAddon"/>'s modes); null for nobody.</summary>
    public int? CarryMode { get; init; }

    /// <summary>How high the move arcs (the client's curve strength: the arc peaks at strength / 100 of a quarter of the
    /// distance; below zero it dips); users moved along jump with it. Null for a straight move.</summary>
    public int? CurveStrength { get; init; }

    /// <summary>How long the furni keeps going past where it stops, in milliseconds at its own speed; null for not.</summary>
    public int? OvershootMilliseconds { get; init; }
}
