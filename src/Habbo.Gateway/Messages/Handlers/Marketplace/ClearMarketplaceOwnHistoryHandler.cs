using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>ClearMarketplaceOwnHistory — [category]: clear the sold or expired offers from the list.</summary>
internal sealed class ClearMarketplaceOwnHistoryHandler(IMarketplaceService service) : IMessageHandler<ClearMarketplaceOwnHistoryMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ClearMarketplaceOwnHistoryMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ClearHistoryAsync(player, message.Category);
        return true;
    }
}
