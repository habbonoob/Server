using Habbo.Protocol.Messages.Incoming.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Furni;

/// <summary>RequestRoomPropertySetEvent — decorate the room with a wallpaper, floor or landscape.</summary>
internal sealed class RequestRoomPropertySetHandler : IMessageHandler<RequestRoomPropertySetEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RequestRoomPropertySetEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        // the client names the item by its strip id, as the inventory gave it
        await room.DecorateAsync(player.Id, InventoryStripId.ItemOf(message.StripId));
        return true;
    }
}
