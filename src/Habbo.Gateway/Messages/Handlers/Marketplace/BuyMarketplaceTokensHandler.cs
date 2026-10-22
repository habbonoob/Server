using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>BuyMarketplaceTokensMessageEvent — buy selling tokens.</summary>
internal sealed class BuyMarketplaceTokensHandler(IMarketplaceService service) : IMessageHandler<BuyMarketplaceTokensMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, BuyMarketplaceTokensMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.BuyTokensAsync(player);
        return true;
    }
}
