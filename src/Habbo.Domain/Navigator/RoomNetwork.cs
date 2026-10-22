namespace Habbo.Domain.Navigator;

/// <summary>A set of rooms entered as one: the client opens a network and is sent to whichever of
/// its rooms has space.</summary>
public sealed class RoomNetwork
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>The network's rooms, in the order they fill.</summary>
    public List<int> RoomIds { get; set; } = [];
}
