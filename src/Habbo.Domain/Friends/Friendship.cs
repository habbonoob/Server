using Habbo.Domain.Users;

namespace Habbo.Domain.Friends;

/// <summary>One side of a friendship. Friendship is mutual, but what each side does with it is
/// not — they file it under their own category and mark their own relationship — so accepting a
/// request writes a row each way rather than one shared row.</summary>
public sealed class Friendship
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid FriendId { get; set; }
    public User? Friend { get; set; }

    /// <summary>The owner's category for this friend, or null for none.</summary>
    public int? CategoryId { get; set; }
    public FriendCategory? Category { get; set; }

    public RelationshipStatus Relationship { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
