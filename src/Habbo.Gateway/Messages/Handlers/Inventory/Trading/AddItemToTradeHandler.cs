using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Incoming.Inventory.Trading;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Trading;

/// <summary>AddItemToTradeEvent — offer an item.</summary>
internal sealed class AddItemToTradeHandler : IMessageHandler<AddItemToTradeEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, AddItemToTradeEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.AddItemsToTradeAsync(player.Id, [InventoryStripId.ItemOf(message.ItemId)]);
        return true;
    }
}
