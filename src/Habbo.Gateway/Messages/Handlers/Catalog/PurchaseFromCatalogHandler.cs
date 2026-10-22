using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Players;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>1962 PurchaseFromCatalog — buy an offer. The client leaves its spinner up until it
/// gets PurchaseOK or PurchaseError, so every path here has to answer with one of the two.
/// The new balance and the new items are pushed after, because the client does not re-ask.</summary>
internal sealed class PurchaseFromCatalogHandler(
    IShopCatalog catalog,
    IGrainFactory grains,
    ILogger<PurchaseFromCatalogHandler> logger) : IMessageHandler<PurchaseFromCatalogEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, PurchaseFromCatalogEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null)
            return false;

        var offer = await catalog.GetPurchasableOfferAsync(message.OfferId, player.Rank, ct);
        if (offer is null)
        {
            logger.LogWarning("Connection {ConnectionId}: purchase of unknown offer {Offer}",
                context.Session.Id, message.OfferId);
            await context.SendAsync(PurchaseErrorMessageComposer.Failed(), ct);
            return true;
        }

        // a limited edition just gone on sale is raffled among those who try for it first; the draw answers them
        if (offer.IsLimited && await grains.GetGrain<ILtdRaffleGrain>(offer.OfferId).EnterAsync(player.Id, offer, message.ExtraData))
            return true;

        var playerGrain = grains.GetGrain<IPlayerGrain>(player.Id);
        var result = await playerGrain.PurchaseAsync(offer, message.Amount, message.ExtraData);
        if (!result.Success)
        {
            logger.LogInformation("Connection {ConnectionId}: purchase of offer {Offer} refused ({Reason})",
                context.Session.Id, offer.OfferId, result.Failure);
            await context.SendAsync(CatalogRefusals.For(result), ct);
            return true;
        }

        // The session caches the player from login, so the debit has to be reflected there too
        // or anything reading it later reports the old balance.
        context.Session.RefreshCredits(result.RemainingCredits);

        // Habbo's order: the balance, then the confirmation, then the items
        await context.SendAsync(new CreditBalanceComposer(result.RemainingCredits), ct);
        await context.SendAsync(new PurchaseOKMessageComposer(
            PurchaseOfferData.From(CatalogOfferFactory.From(offer)), result.ItemIds ?? []), ct);

        // The client merges the new rows into the inventory it holds, and marks them unseen.
        await playerGrain.AnnounceItemsAsync(result.ItemIds ?? []);

        // a membership bought changes the player the session holds
        if (offer.Products.Any(p => p.Type == ProductTypes.Club))
            context.Session.RefreshPlayer(await playerGrain.GetDataAsync());

        return true;
    }
}
