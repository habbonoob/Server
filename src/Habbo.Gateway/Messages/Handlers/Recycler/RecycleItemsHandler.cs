using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Incoming.Recycler;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Recycler;

/// <summary>RecycleItemsMessageEvent — recycle items.</summary>
internal sealed class RecycleItemsHandler(IRecyclerService service) : IMessageHandler<RecycleItemsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RecycleItemsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.RecycleAsync(player, message.ItemIds.Select(InventoryStripId.ItemOf).ToList());
        return true;
    }
}
