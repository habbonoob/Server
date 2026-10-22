using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>PurchaseRoomAd — buy a room's event, or more time for it. Like any purchase, the client
/// waits for PurchaseOK or one of the refusals.</summary>
internal sealed class PurchaseRoomAdHandler(IRoomAdService roomAds, IShopCatalog catalog) : IMessageHandler<PurchaseRoomAdMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PurchaseRoomAdMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var result = await roomAds.PurchaseAsync(player, message.OfferId, message.FlatId, message.Name, message.Extended,
            message.Description, message.CategoryId);
        if (!result.Success || await catalog.GetOfferAsync(message.OfferId, ct) is not { } offer)
        {
            await context.SendAsync(CatalogRefusals.For(result), ct);
            return true;
        }

        context.Session.RefreshCredits(result.RemainingCredits);
        await context.SendAsync(new CreditBalanceComposer(result.RemainingCredits), ct);
        await context.SendAsync(new PurchaseOKMessageComposer(PurchaseOfferData.From(CatalogOfferFactory.From(offer)), []), ct);
        return true;
    }
}
