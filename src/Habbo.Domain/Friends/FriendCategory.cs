using Habbo.Domain.Users;

namespace Habbo.Domain.Friends;

/// <summary>A group a player files friends under. Each player names their own, so a category
/// belongs to one player and means nothing to anybody else.</summary>
public sealed class FriendCategory
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public required string Name { get; set; }
}
