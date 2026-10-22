using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetCatalogPageExpirationEvent — when a page stops being sold.</summary>
internal sealed class GetCatalogPageExpirationHandler(ICatalogExtrasService service) : IMessageHandler<GetCatalogPageExpirationEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetCatalogPageExpirationEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendPageExpirationAsync(player, message.PageName);
        return true;
    }
}
