using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>CancelAllMarketplaceOffers — take every open offer back.</summary>
internal sealed class CancelAllMarketplaceOffersHandler(IMarketplaceService service) : IMessageHandler<CancelAllMarketplaceOffersMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CancelAllMarketplaceOffersMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.CancelAllAsync(player);
        return true;
    }
}
