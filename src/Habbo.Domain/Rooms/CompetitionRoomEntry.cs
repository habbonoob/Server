using Habbo.Domain.Users;

namespace Habbo.Domain.Rooms;

/// <summary>A room entered into a room competition's goal.</summary>
public sealed class CompetitionRoomEntry
{
    public int Id { get; set; }

    public int GoalId { get; set; }

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int Votes { get; set; }

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
}
