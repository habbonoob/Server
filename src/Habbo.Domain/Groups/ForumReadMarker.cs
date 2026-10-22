namespace Habbo.Domain.Groups;

/// <summary>How far into a forum a player has read: the forum's message count when they last looked.</summary>
public sealed class ForumReadMarker
{
    public Guid UserId { get; set; }

    public int GroupId { get; set; }

    public int LastReadMessageIndex { get; set; }
}
