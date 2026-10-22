namespace Habbo.Domain.Rooms;

/// <summary>Somebody whose chat a room does not carry, until when.</summary>
public sealed class RoomMute
{
    public int RoomId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
