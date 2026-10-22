using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>GetMarketplaceCanMakeOfferMessageEvent — whether the player may sell.</summary>
internal sealed class GetMarketplaceCanMakeOfferHandler(IMarketplaceService service) : IMessageHandler<GetMarketplaceCanMakeOfferMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetMarketplaceCanMakeOfferMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendCanMakeOfferAsync(player);
        return true;
    }
}
