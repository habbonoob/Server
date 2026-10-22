using Habbo.Domain.Users;

namespace Habbo.Domain.Groups;

/// <summary>Somebody asking to join an exclusive group.</summary>
public sealed class GroupMembershipRequest
{
    public int GroupId { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
}
