using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Players;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>PurchaseFromCatalogAsGift — buy one of an offer, wrapped, for somebody else. Like a purchase,
/// the client waits for PurchaseOK or one of the refusals.</summary>
internal sealed class PurchaseFromCatalogAsGiftHandler(IShopCatalog catalog, IGrainFactory grains)
    : IMessageHandler<PurchaseFromCatalogAsGiftEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PurchaseFromCatalogAsGiftEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        // a limited edition in its raffle is only to be had through the raffle, not as a gift to someone else
        if (await catalog.GetPurchasableOfferAsync(message.OfferId, player.Rank, ct) is not { } offer
            || (offer.IsLimited && await grains.GetGrain<ILtdRaffleGrain>(offer.OfferId).IsOpenAsync(offer)))
        {
            await context.SendAsync(PurchaseErrorMessageComposer.Failed(), ct);
            return true;
        }

        var result = await grains.GetGrain<IPlayerGrain>(player.Id).PurchaseGiftAsync(offer, message.ExtraData,
            new GiftWrapping(message.ReceiverName, message.Message, message.SpriteId, message.BoxId, message.RibbonId, message.ShowPurchaserName));
        if (!result.Success)
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
