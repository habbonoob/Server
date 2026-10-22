using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetProductOffer — one offer, for a link or an ad that names it; nothing for one that is gone.</summary>
internal sealed class GetProductOfferHandler(IShopCatalog catalog) : IMessageHandler<GetProductOfferEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetProductOfferEvent message, CancellationToken ct)
    {
        if (context.Session.Player is null)
            return false;

        if (await catalog.GetOfferAsync(message.OfferId, ct) is { } offer)
            await context.SendAsync(new ProductOfferComposer(CatalogOfferFactory.From(offer)), ct);
        return true;
    }
}
