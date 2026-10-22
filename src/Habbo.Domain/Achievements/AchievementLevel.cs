namespace Habbo.Domain.Achievements;

/// <summary>One step of an <see cref="Achievement"/>: how far a player has to get, and what
/// reaching it pays.</summary>
public sealed class AchievementLevel
{
    public int Id { get; set; }

    public int AchievementId { get; set; }
    public Achievement? Achievement { get; set; }

    /// <summary>Levels count from one.</summary>
    public int Level { get; set; }

    /// <summary>The badge reaching this level awards. Usually the achievement's name with the
    /// level on the end, but not always — ACH_VipParties1_Entry carries no level at all — so it
    /// is recorded per level rather than worked out.</summary>
    public string BadgeCode { get; set; } = string.Empty;

    /// <summary>The total progress this level is reached at, counted from zero rather than from
    /// the level before — which is how the client reads it off the wire.</summary>
    public int RequiredProgress { get; set; }

    /// <summary>What reaching this level pays, in the achievement's reward currency.</summary>
    public int RewardPoints { get; set; }
}
