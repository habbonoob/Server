namespace Habbo.Domain.Rooms;

/// <summary>A user's like of a room. One per user per room; the room's score counts them.</summary>
public sealed class RoomVote
{
    public int RoomId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
