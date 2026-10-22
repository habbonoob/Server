using Habbo.Domain.Users;

namespace Habbo.Domain.Rooms;

/// <summary>A room a user has starred in the navigator.</summary>
public sealed class RoomFavourite
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}
