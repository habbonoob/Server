using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>GetNftMarketplaceItemStatsMessageEvent — an NFT product's price history.</summary>
internal sealed class GetNftMarketplaceItemStatsHandler(IMarketplaceService service) : IMessageHandler<GetNftMarketplaceItemStatsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetNftMarketplaceItemStatsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendNftItemStatsAsync(player, message.ProductCode);
        return true;
    }
}
