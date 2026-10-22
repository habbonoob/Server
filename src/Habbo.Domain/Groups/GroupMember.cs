using Habbo.Domain.Users;

namespace Habbo.Domain.Groups;

/// <summary>A member of a group.</summary>
public sealed class GroupMember
{
    public int GroupId { get; set; }
    public Group? Group { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public GroupRank Rank { get; set; } = GroupRank.Member;

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
