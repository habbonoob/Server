using Habbo.Domain.Users;

namespace Habbo.Domain.Friends;

/// <summary>A pending request from one player to another. It lives until accepted or declined;
/// accepting turns it into a <see cref="Friendship"/> each way.</summary>
public sealed class FriendRequest
{
    public int Id { get; set; }

    public Guid RequesterId { get; set; }
    public User? Requester { get; set; }

    public Guid TargetId { get; set; }
    public User? Target { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
