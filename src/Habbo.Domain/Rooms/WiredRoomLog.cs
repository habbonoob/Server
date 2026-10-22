namespace Habbo.Domain.Rooms;

/// <summary>A line of a room's wired log, as the wired menu lists them: written by a "write to
/// logs" effect, or by the wired engine itself.</summary>
public sealed class WiredRoomLog
{
    /// <summary>The longest line kept; the effect's dialog allows 400 characters.</summary>
    public const int MessageMaxLength = 1024;

    public long Id { get; set; }

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    /// <summary>INFO, WARN, ERROR or DEBUG (wiredmenu.logs_overview.log_level.N).</summary>
    public byte Level { get; set; }

    /// <summary>System or Wired (wiredmenu.logs_overview.log_source.N).</summary>
    public byte Source { get; set; }

    public required string Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
