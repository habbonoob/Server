namespace Habbo.Silo.Rooms;

/// <summary>An effect a chat command switches on and off: the effect, by its library in the effect map, and whether
/// only club members may wear it.</summary>
public sealed class RoomChatEffect
{
    public string Library { get; set; } = string.Empty;

    public bool ClubOnly { get; set; }
}
