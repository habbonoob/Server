using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetLimitedOfferAppearingNextMessageEvent — the next limited edition.</summary>
internal sealed class GetLimitedOfferAppearingNextHandler(ICatalogExtrasService service) : IMessageHandler<GetLimitedOfferAppearingNextMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetLimitedOfferAppearingNextMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendLimitedOfferAppearingNextAsync(player);
        return true;
    }
}
