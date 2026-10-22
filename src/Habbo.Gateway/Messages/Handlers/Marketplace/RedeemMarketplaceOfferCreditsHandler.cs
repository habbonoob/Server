using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>RedeemMarketplaceOfferCreditsMessageEvent — collect sales.</summary>
internal sealed class RedeemMarketplaceOfferCreditsHandler(IMarketplaceService service) : IMessageHandler<RedeemMarketplaceOfferCreditsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RedeemMarketplaceOfferCreditsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.RedeemCreditsAsync(player);
        return true;
    }
}
