using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>CancelMarketplaceOfferMessageEvent — take an item back.</summary>
internal sealed class CancelMarketplaceOfferHandler(IMarketplaceService service) : IMessageHandler<CancelMarketplaceOfferMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CancelMarketplaceOfferMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.CancelAsync(player, message.OfferId);
        return true;
    }
}
