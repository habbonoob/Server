namespace Habbo.Domain.Moderation;

/// <summary>A line said in a room, kept for the mod tool's chat logs.</summary>
public sealed class RoomChatLine
{
    public const int TextMaxLength = 512;

    public long Id { get; set; }

    public int RoomId { get; set; }

    public Guid UserId { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset SaidAt { get; set; } = DateTimeOffset.UtcNow;
}
