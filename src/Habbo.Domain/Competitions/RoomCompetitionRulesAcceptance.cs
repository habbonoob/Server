namespace Habbo.Domain.Competitions;

/// <summary>A player's having read and accepted a room competition's rules, which entering a room waits on.</summary>
public sealed class RoomCompetitionRulesAcceptance
{
    public int GoalId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset AcceptedAt { get; set; } = DateTimeOffset.UtcNow;
}
