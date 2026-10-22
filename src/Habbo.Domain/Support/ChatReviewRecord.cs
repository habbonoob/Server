namespace Habbo.Domain.Support;

/// <summary>A bully report the guardians judged: who reported whom, where, when, and the verdict, by the
/// client's result codes (ok, bad, very bad, inconclusive). Kept so a reporter's pace and the reports of
/// theirs that did not stand can be counted.</summary>
public sealed class ChatReviewRecord
{
    public int Id { get; set; }

    public Guid ReporterId { get; set; }

    public Guid ReportedId { get; set; }

    public int RoomId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int Verdict { get; set; }
}
