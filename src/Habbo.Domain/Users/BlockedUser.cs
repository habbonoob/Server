namespace Habbo.Domain.Users;

/// <summary>Somebody a player has blocked, which stops them following or messaging. Muting is
/// a separate list — see <see cref="IgnoredUser"/>.</summary>
public sealed class BlockedUser
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid BlockedUserId { get; set; }
    public User? Blocked { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
