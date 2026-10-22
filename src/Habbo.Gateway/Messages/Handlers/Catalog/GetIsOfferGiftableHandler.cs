using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetIsOfferGiftable — whether an offer may be bought as a gift.</summary>
internal sealed class GetIsOfferGiftableHandler(IShopCatalog catalog) : IMessageHandler<GetIsOfferGiftableEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetIsOfferGiftableEvent message, CancellationToken ct)
    {
        if (context.Session.Player is null)
            return false;

        var offer = await catalog.GetOfferAsync(message.OfferId, ct);
        await context.SendAsync(new IsOfferGiftableMessageComposer(message.OfferId, offer is { Giftable: true, IsLimited: false }), ct);
        return true;
    }
}
