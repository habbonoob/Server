namespace Habbo.Domain.Competitions;

/// <summary>A room entered in a room competition. Each player enters one room at most, and it cannot be taken back.</summary>
public sealed class RoomCompetitionEntry
{
    /// <summary>The competition, by the hotel's goal id.</summary>
    public int GoalId { get; set; }

    public int RoomId { get; set; }

    /// <summary>Who entered it, the room's owner.</summary>
    public Guid UserId { get; set; }

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
}
