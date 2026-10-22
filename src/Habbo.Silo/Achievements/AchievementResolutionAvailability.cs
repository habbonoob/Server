namespace Habbo.Silo.Achievements;

/// <summary>Whether an achievement can be a statue's challenge; the client explains the others with
/// resolution.disabled.&lt;value&gt;.</summary>
public enum AchievementResolutionAvailability
{
    Available = 0,

    /// <summary>Every level of it is done.</summary>
    AllLevelsDone = 1,

    /// <summary>Another of the player's statues is already waiting on it.</summary>
    AlreadyChallenged = 2,
}
