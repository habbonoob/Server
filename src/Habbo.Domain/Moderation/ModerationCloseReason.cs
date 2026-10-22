namespace Habbo.Domain.Moderation;

/// <summary>Why an issue was closed, as the reporter's close notification words it.</summary>
public enum ModerationCloseReason
{
    Useless = 1,
    Abusive = 2,
    Resolved = 3,
}
