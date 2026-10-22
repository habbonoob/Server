using Habbo.Domain.Users;

namespace Habbo.Domain.Groups;

/// <summary>Somebody a group's admins keep out: they may not join or ask to.</summary>
public sealed class GroupBlock
{
    public int GroupId { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public DateTimeOffset BlockedAt { get; set; } = DateTimeOffset.UtcNow;
}
