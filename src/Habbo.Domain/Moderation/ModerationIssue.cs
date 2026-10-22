using Habbo.Domain.Users;

namespace Habbo.Domain.Moderation;

/// <summary>A call for help, as the mod tool works it: somebody reported somebody (or a room, a
/// forum post, a photo), a moderator picks it up, and it is closed with a verdict.</summary>
public sealed class ModerationIssue
{
    public const int MessageMaxLength = 512;

    public int Id { get; set; }

    public ModerationIssueState State { get; set; } = ModerationIssueState.Open;

    /// <summary>Where the report came from (normal, instant messages, room, forum, photo…), as the
    /// mod tool names the <see cref="ModerationIssueSources"/>.</summary>
    public int Source { get; set; }

    /// <summary>The help topic the reporter picked.</summary>
    public int TopicId { get; set; }

    public Guid? ReporterId { get; set; }
    public User? Reporter { get; set; }

    public Guid? ReportedUserId { get; set; }
    public User? ReportedUser { get; set; }

    public int? RoomId { get; set; }

    public int? GroupId { get; set; }
    public int? ThreadId { get; set; }
    public int? ForumMessageId { get; set; }

    /// <summary>The photo or selfie reported, by the client's id for it.</summary>
    public string PhotoId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>What the reporter gave of themselves for the unlawful topics.</summary>
    public string ReporterName { get; set; } = string.Empty;
    public string ReporterEmail { get; set; } = string.Empty;

    public Guid? PickerId { get; set; }
    public User? Picker { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ClosedAt { get; set; }

    public ModerationCloseReason? CloseReason { get; set; }

    /// <summary>Whether closing it sanctioned somebody.</summary>
    public bool Sanctioned { get; set; }

    public ModerationAppealState AppealState { get; set; } = ModerationAppealState.None;
    public DateTimeOffset? AppealedAt { get; set; }
    public DateTimeOffset? AppealResolvedAt { get; set; }

    public List<ModerationIssueChatLine> ChatLines { get; set; } = [];
}
