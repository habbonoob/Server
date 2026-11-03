using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>BuyNftMarketplaceOfferMessageEvent — buy the cheapest NFT on sale of a product, for emeralds.</summary>
internal sealed class BuyNftMarketplaceOfferHandler(IMarketplaceService service) : IMessageHandler<BuyNftMarketplaceOfferMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, BuyNftMarketplaceOfferMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.BuyNftAsync(player, message.ProductCode, message.Price);
        return true;
    }
}
