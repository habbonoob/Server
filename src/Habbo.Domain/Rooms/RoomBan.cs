namespace Habbo.Domain.Rooms;

/// <summary>Somebody kept out of a room, until when.</summary>
public sealed class RoomBan
{
    public int RoomId { get; set; }

    public Guid UserId { get; set; }
    public Users.User? User { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
