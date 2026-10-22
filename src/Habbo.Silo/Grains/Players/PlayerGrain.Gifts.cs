using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Inventory;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Items;
using Habbo.Silo.Items.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Silo.Grains.Players;

/// <summary>Gifts: an offer bought for somebody else arrives in their inventory wrapped, with a card;
/// opening it hands over what it holds. The wrapping is one of the hotel's: a free one, or a paid one
/// in a chosen box and ribbon.</summary>
public sealed partial class PlayerGrain
{
    /// <summary>A gift is one of the offer.</summary>
    private const int GiftAmount = 1;

    public async Task<PurchaseResult> PurchaseGiftAsync(ShopOffer offer, string extraData, GiftWrapping wrapping)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Users.FirstOrDefaultAsync(u => u.Id == PlayerId) is not { } buyer)
            return PurchaseResult.Failed(PurchaseFailure.UnknownOffer);
        if (Refuse(offer, GiftAmount, buyer) is { } refused)
            return refused;
        if (!await PetOrderAcceptedAsync(db, offer, extraData) || !await FurniOrderAcceptedAsync(db, offer, extraData))
            return PurchaseResult.Failed(PurchaseFailure.NotAllowed);

        var wraps = await giftWrapping.GetAsync();
        var paidWrap = wraps.WrapClassIds.Contains(wrapping.SpriteId);
        var freeWrap = wraps.DefaultClassIds.Contains(wrapping.SpriteId);
        if (!offer.Giftable || !wraps.Enabled || !(paidWrap || freeWrap)
            || (paidWrap && (!wraps.BoxTypes.Contains(wrapping.BoxId) || !wraps.RibbonTypes.Contains(wrapping.RibbonId))))
            return PurchaseResult.Failed(PurchaseFailure.NotAllowed);

        var receiverName = wrapping.ReceiverName.Trim();
        var receiver = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username.ToLower() == receiverName.ToLower());
        if (receiver is null)
            return PurchaseResult.Failed(PurchaseFailure.ReceiverNotFound);

        var wrap = await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.ClassId == wrapping.SpriteId && d.Type == ItemType.Floor);
        if (wrap is null)
            return PurchaseResult.Failed(PurchaseFailure.NotAllowed);

        await using var transaction = await db.Database.BeginTransactionAsync();
        var charge = await ChargeAsync(db, buyer, offer, GiftAmount, extraCredits: paidWrap ? wraps.Price : 0);
        if (charge.Failure != PurchaseFailure.None)
            return PurchaseResult.Failed(charge.Failure) with { ActivityPointType = offer.ActivityPointType };

        var present = new UserItem
        {
            OwnerId = receiver.Id,
            DefinitionId = wrap.Id,
            Extra = paidWrap ? wrapping.BoxId * PresentInteraction.BoxFactor + wrapping.RibbonId : 0,
        };
        db.UserItems.Add(present);
        FurniDataStore.Attach(db, present, Card(offer, buyer, wrapping, await GiftMessageAsync(wrapping.Message)));
        db.FurniGifts.Add(new FurniGift
        {
            Item = present, SenderId = buyer.Id, OfferId = offer.OfferId,
            ExtraData = extraData.Length > FurniGift.MaxExtraDataLength ? extraData[..FurniGift.MaxExtraDataLength] : extraData,
            LimitedSerial = charge.Serial, LimitedStack = offer.LimitedStack,
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        await AnnounceChargeAsync(buyer, offer, charge);
        await GrainFactory.GetGrain<IPlayerGrain>(receiver.Id).AnnounceItemsAsync([present.Id]);
        if (receiver.Id != PlayerId)
        {
            await ProgressAchievementAsync(Achievements.AchievementNames.GiftGiver, 1);
            await GrainFactory.GetGrain<IPlayerGrain>(receiver.Id).ProgressAchievementAsync(Achievements.AchievementNames.GiftReceiver, 1);
        }

        // a pet bought as a gift counts for whoever bought it
        if (offer.Products.Any(product => product.Type == ProductTypes.Pet))
            await ProgressAchievementAsync(Achievements.AchievementNames.PetLover, 1);
        logger.LogInformation("Player {PlayerId} gave offer {Offer} to {Receiver}.", PlayerId, offer.OfferId, receiver.Username);

        return new PurchaseResult(PurchaseFailure.None, offer, buyer.Credits, 0);
    }

    /// <summary>The card on the present: the message, what it holds, and who sent it if they signed it.</summary>
    private static FurniDataView Card(ShopOffer offer, Habbo.Domain.Users.User buyer, GiftWrapping wrapping, string message)
    {
        var entries = new List<FurniMapEntryView>
        {
            new(PresentInteraction.MessageKey, message),
            new(PresentInteraction.ProductCodeKey, offer.LocalizationId),
        };
        if (wrapping.ShowPurchaserName)
        {
            entries.Add(new(PresentInteraction.PurchaserNameKey, buyer.Username));
            entries.Add(new(PresentInteraction.PurchaserFigureKey, buyer.Figure));
        }

        return FurniDataView.Plain with { Type = (int)FurniDataType.Map, State = null, MapEntries = entries };
    }

    private async Task<string> GiftMessageAsync(string message)
    {
        var limit = giftWrappingOptions.Value.MessageMaxLength;
        return (await wordFilter.FilterAsync(message.Length > limit ? message[..limit] : message, [])).Text;
    }

    public async Task<UnwrappedGift?> UnwrapGiftAsync(int presentId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var gift = await db.FurniGifts.Include(g => g.Item).FirstOrDefaultAsync(g => g.ItemId == presentId && g.Item!.OwnerId == PlayerId);
        if (gift is null || await db.Users.FirstOrDefaultAsync(u => u.Id == PlayerId) is not { } owner)
            return null;

        // a prize box holds one item; a present holds an offer's products
        var offer = gift.OfferId is { } offerId ? await shopCatalog.GetOfferAsync(offerId)
            : gift.PrizeDefinitionId is { } prize ? PrizeOffer(prize, string.Empty, 1)
            : null;
        if (offer is null)
            return null;

        await using var transaction = await db.Database.BeginTransactionAsync();
        var granted = await GrantProductsAsync(db, offer, GiftAmount, gift.ExtraData, owner, gift.LimitedSerial, gift.LimitedStack);
        await db.SaveChangesAsync();
        await db.UserItems.Where(i => i.Id == presentId).ExecuteDeleteAsync();
        await transaction.CommitAsync();

        await AnnounceGrantAsync(granted, asGift: true);

        var first = granted.Created.FirstOrDefault();
        var definition = first is null ? null : await db.ItemDefinitions.AsNoTracking().FirstAsync(d => d.Id == first.DefinitionId);
        var itemType = definition is null && granted.Bots.Count > 0 ? Habbo.Protocol.Messages.Outgoing.Catalog.CatalogProductData.BotType
            : definition?.Type == ItemType.Wall ? Habbo.Protocol.Messages.Outgoing.Catalog.CatalogProductData.WallType
            : Habbo.Protocol.Messages.Outgoing.Catalog.CatalogProductData.FloorType;
        return new UnwrappedGift(
            granted.Created.Select(i => i.Id).ToList(),
            itemType,
            definition?.ClassId ?? 0,
            definition?.ClassName ?? offer.LocalizationId,
            definition?.Type == ItemType.Floor);
    }

    public async Task<bool> ReceivePrizeGiftAsync(int definitionId, string message, Guid senderId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var prize = await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == definitionId);
        var sender = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == senderId);
        var wraps = await giftWrapping.GetAsync();
        var wrap = wraps.DefaultClassIds.Count == 0 ? null
            : await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => wraps.DefaultClassIds.Contains(d.ClassId) && d.Type == ItemType.Floor);
        if (prize is null || sender is null || wrap is null)
            return false;

        // a free wrap, signed by whoever gave it, holding the one item
        var present = new UserItem { OwnerId = PlayerId, DefinitionId = wrap.Id };
        db.UserItems.Add(present);
        FurniDataStore.Attach(db, present, FurniDataView.Plain with
        {
            Type = (int)FurniDataType.Map,
            State = null,
            MapEntries =
            [
                new(PresentInteraction.MessageKey, await GiftMessageAsync(message)),
                new(PresentInteraction.ProductCodeKey, prize.ClassName),
                new(PresentInteraction.PurchaserNameKey, sender.Username),
                new(PresentInteraction.PurchaserFigureKey, sender.Figure),
            ],
        });
        db.FurniGifts.Add(new FurniGift { Item = present, SenderId = sender.Id, PrizeDefinitionId = prize.Id });
        await db.SaveChangesAsync();

        await AnnounceItemsAsync([present.Id]);
        return true;
    }

    public async Task<IReadOnlyList<int>> ReceiveFurnitureAsync(int definitionId, string productParam, int amount)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var owner = await LoadUserAsync(db);
        var granted = await GrantProductsAsync(db, PrizeOffer(definitionId, productParam, amount), GiftAmount, string.Empty, owner, serial: null);
        await db.SaveChangesAsync();

        var itemIds = granted.Created.Select(i => i.Id).ToList();
        await AnnounceItemsAsync(itemIds);
        return itemIds;
    }

    /// <summary>So many of one item, with the parameter its product gives it (a poster's number), as an offer to hand over.</summary>
    private static ShopOffer PrizeOffer(int definitionId, string productParam, int amount) => new(0, string.Empty, 0, Habbo.Domain.Users.ClubLevel.None, 1,
        [new ShopProduct(definitionId, 0, false, productParam, amount)], 0, 0, 0, false, false, false, string.Empty);

    public Task AnnounceItemsAsync(List<int> itemIds) => SendItemsAsync(itemIds, unseen: true);

    public Task RestoreItemsAsync(List<int> itemIds) => SendItemsAsync(itemIds, unseen: false);

    private async Task SendItemsAsync(List<int> itemIds, bool unseen)
    {
        if (itemIds.Count == 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var items = await db.UserItems.AsNoTracking().Include(i => i.Definition)
            .Where(i => itemIds.Contains(i.Id) && i.OwnerId == PlayerId && i.RoomId == null && !i.InMarketplace && i.ChestId == null).ToListAsync();
        var data = await furniData.LoadAsync(db, items);
        var views = items.Where(i => i.Definition is not null)
            .Select(i => InventoryFurniData.From(InventoryProjection.From(i, i.Definition!, data[i.Id]))).ToList();
        if (views.Count == 0)
            return;

        // Habbo marks them unseen before it hands them over
        if (unseen)
            Send(UnseenItemsComposer.OwnedFurni(items.Select(i => i.Id).ToList()));
        Send(new FurniListAddOrUpdateComposer(views));
    }
}
