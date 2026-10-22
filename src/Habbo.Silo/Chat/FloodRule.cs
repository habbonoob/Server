namespace Habbo.Silo.Chat;

/// <summary>One flood-control sensitivity: lines within the window, and the mute they earn.</summary>
public sealed class FloodRule
{
    public int Sensitivity { get; set; }

    public int MaxLines { get; set; }

    public int WindowSeconds { get; set; }

    public int PenaltySeconds { get; set; }
}
