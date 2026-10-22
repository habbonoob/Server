namespace Habbo.Domain.Users;

/// <summary>Somebody a player has muted: the client hides their chat. Kept apart from
/// <see cref="BlockedUser"/> because the client tracks the two separately and they do
/// different things.</summary>
public sealed class IgnoredUser
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid IgnoredUserId { get; set; }
    public User? Ignored { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
