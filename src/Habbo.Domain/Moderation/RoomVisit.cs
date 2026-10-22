namespace Habbo.Domain.Moderation;

/// <summary>A user going into a room, kept for the mod tool's room visits and chat logs.</summary>
public sealed class RoomVisit
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public int RoomId { get; set; }

    public DateTimeOffset EnteredAt { get; set; } = DateTimeOffset.UtcNow;
}
