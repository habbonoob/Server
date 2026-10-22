using Habbo.Domain.Users;

namespace Habbo.Domain.Rooms;

/// <summary>A user the room's owner has given rights in the room: they may place, move and use
/// furniture there as the owner does, and pick up their own.</summary>
public sealed class RoomRight
{
    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
}
