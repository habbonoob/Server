namespace Habbo.Domain.Moderation;

/// <summary>One step of the sanction ladder a default sanction climbs: an alert, then a mute, then
/// ever longer bans. The ids are the ones the mod tool's actions ask for.</summary>
public sealed class SanctionType
{
    public int Id { get; set; }

    /// <summary>The name the client shows ("ALERT", "MUTE", "BAN").</summary>
    public string Name { get; set; } = string.Empty;

    public SanctionKind Kind { get; set; }

    public int LengthHours { get; set; }

    /// <summary>How long after it the user is on probation, when the next default sanction climbs on.</summary>
    public int ProbationDays { get; set; }

    /// <summary>The step a default sanction takes a user to next; its place on the ladder.</summary>
    public int Step { get; set; }
}
