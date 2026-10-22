namespace Habbo.Domain.Rooms;

/// <summary>One look at, or click on, a room's promotion: what the promotion's buyer paid for.</summary>
public sealed class RoomAdInteraction
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public RoomAdInteractionKind Kind { get; set; }

    /// <summary>The promotion concerned; none for a look at the whole tab.</summary>
    public int? EventId { get; set; }

    public int? RoomId { get; set; }

    /// <summary>Where it came from: the promoted category and the room's place in it.</summary>
    public string Source { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}
