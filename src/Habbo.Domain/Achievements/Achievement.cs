namespace Habbo.Domain.Achievements;

/// <summary>Something a player works towards over several levels, each level worth a badge and
/// a handful of points. The client draws them grouped by category, so both the grouping and the
/// levels are data rather than code.</summary>
public sealed class Achievement
{
    public int Id { get; set; }

    /// <summary>What to call it. Only for reading the table by eye: the badge a player holds
    /// belongs to the level, not to the achievement, and two achievements can share a name stem —
    /// ACH_Profile1 and ACH_Profile2 are different achievements, both on their first level.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>The group the client files it under, e.g. "explore".</summary>
    public required string Category { get; set; }

    /// <summary>A finer grouping inside the category; empty when there is none.</summary>
    public string SubCategory { get; set; } = string.Empty;

    /// <summary>Which of the client's progress readouts to use — a bar, a counter, and so on.
    /// </summary>
    public int DisplayMethod { get; set; }

    /// <summary>Which currency the level rewards are paid in.</summary>
    public int RewardPointType { get; set; }

    /// <summary>How many levels it has in total. Its own value rather than a count of the rows
    /// below, because the hotel can know an achievement runs to twenty levels while only some of
    /// their requirements are on record.</summary>
    public int LevelCount { get; set; }

    /// <summary>What the client does with it. The only value it acts on is
    /// <see cref="AchievementState.Archived"/>, which files the achievement under the archive
    /// tab instead of its own category; everything else it draws the same way.</summary>
    public AchievementState State { get; set; } = AchievementState.Active;

    public List<AchievementLevel> Levels { get; set; } = [];

    /// <summary>How many levels it runs to: its own count, or more when more are on record.</summary>
    public int TotalLevels => Math.Max(LevelCount, Levels.Count == 0 ? 0 : Levels.Max(l => l.Level));

    /// <summary>A level's badge; one not on record takes the usual shape, the achievement's name and the level.</summary>
    public string BadgeOf(AchievementLevel level) => level.BadgeCode.Length > 0 ? level.BadgeCode : Name + level.Level;
}
