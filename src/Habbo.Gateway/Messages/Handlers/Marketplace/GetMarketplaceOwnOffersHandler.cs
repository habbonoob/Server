using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>GetMarketplaceOwnOffersMessageEvent — the player's own offers.</summary>
internal sealed class GetMarketplaceOwnOffersHandler(IMarketplaceService service) : IMessageHandler<GetMarketplaceOwnOffersMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetMarketplaceOwnOffersMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendOwnOffersAsync(player, message.Category);
        return true;
    }
}
