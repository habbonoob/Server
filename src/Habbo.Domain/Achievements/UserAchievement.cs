using Habbo.Domain.Users;

namespace Habbo.Domain.Achievements;

/// <summary>How far one player has got with one achievement.</summary>
public sealed class UserAchievement
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int AchievementId { get; set; }
    public Achievement? Achievement { get; set; }

    /// <summary>The highest level reached, zero while the first one is still being worked on.</summary>
    public int Level { get; set; }

    /// <summary>Total progress so far, counted from zero across every level.</summary>
    public int Progress { get; set; }
}
