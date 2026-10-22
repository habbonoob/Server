using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.Marketplace;

/// <summary>GetMarketplaceConfigurationMessageEvent — the marketplace's rules.</summary>
internal sealed class GetMarketplaceConfigurationHandler(IMarketplaceService service) : IMessageHandler<GetMarketplaceConfigurationMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetMarketplaceConfigurationMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendConfigurationAsync(player);
        return true;
    }
}
