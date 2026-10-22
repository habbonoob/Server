using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Incoming.Inventory.Trading;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Trading;

/// <summary>RemoveItemFromTradeEvent — take an item back.</summary>
internal sealed class RemoveItemFromTradeHandler : IMessageHandler<RemoveItemFromTradeEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RemoveItemFromTradeEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.RemoveItemFromTradeAsync(player.Id, InventoryStripId.ItemOf(message.ItemId));
        return true;
    }
}
