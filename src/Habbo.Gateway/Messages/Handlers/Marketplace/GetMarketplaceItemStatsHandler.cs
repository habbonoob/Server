using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>GetMarketplaceItemStatsEvent — an item's price history.</summary>
internal sealed class GetMarketplaceItemStatsHandler(IMarketplaceService service) : IMessageHandler<GetMarketplaceItemStatsEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetMarketplaceItemStatsEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendItemStatsAsync(player, message.FurniType, message.FurniId, message.PosterId);
        return true;
    }
}
