using Habbo.Domain.Users;

namespace Habbo.Domain.Rooms;

/// <summary>A room's promotion (the navigator's "event"): bought for a while, listed under its
/// category, and shown to everyone in the room.</summary>
public sealed class RoomEvent
{
    public const int NameMaxLength = 64;

    public const int DescriptionMaxLength = 255;

    public int Id { get; set; }

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    /// <summary>What kind of promotion was bought.</summary>
    public int Type { get; set; }

    /// <summary>The event category it is listed under.</summary>
    public int CategoryId { get; set; }

    public required string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt { get; set; }

    public bool IsActive(DateTimeOffset now) => ExpiresAt > now;
}
