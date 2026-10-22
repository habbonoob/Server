namespace Habbo.Silo.Achievements;

/// <summary>How the hotel runs resolution statues: how long a challenge lasts, how many levels it asks for, and
/// which achievements a statue offers.</summary>
public sealed class AchievementResolutionOptions
{
    /// <summary>How long the owner has to reach the level once they pick the achievement.</summary>
    public int ChallengeDays { get; set; } = 7;

    /// <summary>How many levels beyond the one the owner is on the challenge asks for.</summary>
    public int LevelsToGain { get; set; } = 1;

    /// <summary>The achievement categories a statue offers; none named offers every live achievement.</summary>
    public List<string> Categories { get; set; } = [];

    public TimeSpan ChallengeDuration => TimeSpan.FromDays(ChallengeDays);
}
