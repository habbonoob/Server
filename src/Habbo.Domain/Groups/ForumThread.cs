using Habbo.Domain.Users;

namespace Habbo.Domain.Groups;

/// <summary>A thread in a group's forum.</summary>
public sealed class ForumThread
{
    public const int SubjectMaxLength = 120;

    public int Id { get; set; }

    public int GroupId { get; set; }

    public Guid AuthorId { get; set; }
    public User? Author { get; set; }

    public required string Subject { get; set; }

    public bool Sticky { get; set; }

    public bool Locked { get; set; }

    public ForumPostState State { get; set; } = ForumPostState.Default;

    /// <summary>Who last hid or restored it, and when.</summary>
    public Guid? AdminId { get; set; }
    public User? Admin { get; set; }
    public DateTimeOffset? AdminActedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<ForumMessage> Messages { get; set; } = [];
}
