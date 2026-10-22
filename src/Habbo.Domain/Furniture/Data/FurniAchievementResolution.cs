namespace Habbo.Domain.Furniture.Data;

/// <summary>The challenge a resolution statue's owner took on: reach a level of an achievement before the time runs
/// out, and the statue is engraved with its badge. There is a row only while the challenge is under way.</summary>
public sealed class FurniAchievementResolution
{
    /// <summary>The statue.</summary>
    public int ItemId { get; set; }

    /// <summary>Who took the challenge on, the statue's owner at the time.</summary>
    public Guid UserId { get; set; }

    public int AchievementId { get; set; }

    /// <summary>The level to reach.</summary>
    public int TargetLevel { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    /// <summary>When the challenge is lost if the level has not been reached.</summary>
    public DateTimeOffset EndsAt { get; set; }
}
