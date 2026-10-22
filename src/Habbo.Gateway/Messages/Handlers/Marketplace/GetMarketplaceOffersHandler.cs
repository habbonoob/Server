using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>GetMarketplaceOffersMessageEvent — search the marketplace.</summary>
internal sealed class GetMarketplaceOffersHandler(IMarketplaceService service) : IMessageHandler<GetMarketplaceOffersMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetMarketplaceOffersMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        // before September 2026 each limited edition number was listed on its own
        await service.SendOffersAsync(player, message.MinPrice, message.MaxPrice, message.SearchQuery, message.SortOrder,
            message.CombineUniques ?? false);
        return true;
    }
}
