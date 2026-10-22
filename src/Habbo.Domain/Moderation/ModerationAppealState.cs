namespace Habbo.Domain.Moderation;

/// <summary>Whether the reporter appealed how their report was closed.</summary>
public enum ModerationAppealState : byte
{
    None = 0,
    Pending = 1,
    Resolved = 2,
}
