namespace Habbo.Domain.Polls;

/// <summary>A player's dealings with a poll: when they turned it down or finished it, and each answer they gave.</summary>
public sealed class PollResponse
{
    public long Id { get; set; }

    public int PollId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>The question answered; null for the player turning the poll down.</summary>
    public int? QuestionId { get; set; }

    public List<string> Answers { get; set; } = [];

    public DateTimeOffset AnsweredAt { get; set; } = DateTimeOffset.UtcNow;
}
