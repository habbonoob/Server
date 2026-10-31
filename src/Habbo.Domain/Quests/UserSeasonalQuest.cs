using Habbo.Domain.Users;

namespace Habbo.Domain.Quests;

/// <summary>A player's progress on one quest of a seasonal campaign (a day of the advent calendar, or a step of a
/// time-limited campaign). The quests themselves are seed data (seed/seasonal_quests.json), named here by id.</summary>
public sealed class UserSeasonalQuest
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int QuestId { get; set; }

    /// <summary>The quest the player is on: at most one at a time, and never a completed one.</summary>
    public bool Accepted { get; set; }

    /// <summary>How many of the quest's steps are done.</summary>
    public int Progress { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
