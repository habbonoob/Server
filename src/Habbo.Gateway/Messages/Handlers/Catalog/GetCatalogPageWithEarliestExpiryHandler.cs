using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetCatalogPageWithEarliestExpiryMessageEvent — the page that stops being sold soonest.</summary>
internal sealed class GetCatalogPageWithEarliestExpiryHandler(ICatalogExtrasService service) : IMessageHandler<GetCatalogPageWithEarliestExpiryMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetCatalogPageWithEarliestExpiryMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendEarliestExpiringPageAsync(player);
        return true;
    }
}
