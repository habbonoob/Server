namespace Habbo.Domain.Competitions;

/// <summary>A vote for a room entered in a room competition.</summary>
public sealed class RoomCompetitionVote
{
    public long Id { get; set; }

    public int GoalId { get; set; }

    public int RoomId { get; set; }

    public Guid VoterId { get; set; }

    public DateTimeOffset VotedAt { get; set; } = DateTimeOffset.UtcNow;
}
