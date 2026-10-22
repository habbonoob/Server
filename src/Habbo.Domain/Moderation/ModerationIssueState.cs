namespace Habbo.Domain.Moderation;

/// <summary>An issue's state, numbered as the client's IssueMessageData reads it.</summary>
public enum ModerationIssueState
{
    Open = 1,
    Picked = 2,
    Closed = 3,
}
