namespace Habbo.Silo.Achievements;

/// <summary>Where a resolution statue is, the first value of its data (FurnitureAchievementResolutionLogic).</summary>
public enum AchievementResolutionState
{
    NotStarted = 0,
    InProgress = 1,

    /// <summary>Done in time: the statue shows the badge, engraved with its owner's name and the date.</summary>
    Achieved = 2,

    Failed = 3,
}
