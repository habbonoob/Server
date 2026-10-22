using Habbo.Domain.Users;

namespace Habbo.Domain.Groups;

/// <summary>A message in a forum thread; the first one opens the thread.</summary>
public sealed class ForumMessage
{
    public const int TextMaxLength = 4000;

    public int Id { get; set; }

    public int ThreadId { get; set; }
    public ForumThread? Thread { get; set; }

    /// <summary>The forum it belongs to, kept here so a forum's messages count without the threads.</summary>
    public int GroupId { get; set; }

    public Guid AuthorId { get; set; }
    public User? Author { get; set; }

    public required string Text { get; set; }

    public ForumPostState State { get; set; } = ForumPostState.Default;

    public Guid? AdminId { get; set; }
    public User? Admin { get; set; }
    public DateTimeOffset? AdminActedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
