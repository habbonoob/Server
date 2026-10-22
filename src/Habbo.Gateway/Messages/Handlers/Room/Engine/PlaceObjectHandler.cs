using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>1743 PlaceObject — one space-joined string: floor "id x y dir", wall "id wallLocation".
/// A wall location has spaces of its own (":w=3,2 l=10,20 r"), so only the id is split off first. The id is the
/// item's inventory strip id.</summary>
internal sealed class PlaceObjectHandler : IMessageHandler<PlaceObjectMessageEvent>
{
    private const int FloorParts = 3;

    /// <summary>What a wall location starts with.</summary>
    private const char WallLocationMarker = ':';

    public async Task<bool> HandleAsync(
        GatewaySessionContext context, PlaceObjectMessageEvent message, CancellationToken ct)
    {
        var idAndRest = message.Placement.Split(' ', 2);

        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room || idAndRest.Length < 2
            || !int.TryParse(idAndRest[0], out var stripId))
            return true;

        var itemId = InventoryStripId.ItemOf(stripId);

        var rest = idAndRest[1];
        if (rest.StartsWith(WallLocationMarker))
        {
            await room.PlaceWallItemAsync(player.Id, itemId, rest);
            return true;
        }

        var parts = rest.Split(' ');
        if (parts.Length == FloorParts && int.TryParse(parts[0], out var x) && int.TryParse(parts[1], out var y)
            && int.TryParse(parts[2], out var rotation))
            await room.PlaceFloorItemAsync(player.Id, itemId, x, y, rotation);

        return true;
    }
}
