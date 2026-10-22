using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Incoming.Inventory.Trading;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Trading;

/// <summary>AddItemsToTradeEvent — offer items.</summary>
internal sealed class AddItemsToTradeHandler : IMessageHandler<AddItemsToTradeEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, AddItemsToTradeEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.AddItemsToTradeAsync(player.Id, message.ItemIds.Select(InventoryStripId.ItemOf).ToList());
        return true;
    }
}
