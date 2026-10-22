namespace Habbo.Domain.Rooms;

/// <summary>A word a room's owner filters out of the room's chat.</summary>
public sealed class RoomFilterWord
{
    public const int WordMaxLength = 64;

    public int RoomId { get; set; }

    public required string Word { get; set; }
}
