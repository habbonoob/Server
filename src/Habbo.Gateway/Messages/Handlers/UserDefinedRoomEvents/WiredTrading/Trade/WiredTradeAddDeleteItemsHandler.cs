using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Trade;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Trade;

/// <summary>WiredTradeAddDeleteItemsMessageEvent — put items into a wired trade or take them out.</summary>
internal sealed class WiredTradeAddDeleteItemsHandler : IMessageHandler<WiredTradeAddDeleteItemsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, WiredTradeAddDeleteItemsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ChangeWiredTradeItemsAsync(player.Id, message.Remove, message.ItemIds.Select(InventoryStripId.ItemOf).ToList());
        return true;
    }
}
