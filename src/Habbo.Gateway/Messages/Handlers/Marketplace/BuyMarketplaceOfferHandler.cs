using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>BuyMarketplaceOfferMessageEvent — buy an offer.</summary>
internal sealed class BuyMarketplaceOfferHandler(IMarketplaceService service) : IMessageHandler<BuyMarketplaceOfferMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, BuyMarketplaceOfferMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.BuyAsync(player, message.OfferId);
        return true;
    }
}
