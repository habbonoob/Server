using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Marketplace;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Protocol.Messages.Outgoing.Marketplace;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Inventory;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Marketplace;

/// <summary>The marketplace. Selling costs a token and puts the item in escrow; a buyer pays the price,
/// the seller collects the price less the selling fee (worked out as the client works it out); an offer
/// expires after its hours and its owner may take the item back.
///
/// November 2026's NFT marketplace runs beside it on the same offers, marked <see cref="MarketplaceOffer.IsNft"/>: an NFT
/// (furni whose class name starts "nft_") is offered alone, for emeralds, under the NFT rules; it is listed, asked about
/// and bought by its product code (<see cref="NftProducts"/>), one search row per product; and the seller is paid the
/// price less the NFT fee in emeralds as it sells, the sale marked redeemed there and then, so no NFT revenue ever waits
/// with the credits to be collected (paying as it sells: habbo.com's help page says the seller receives the emeralds, less the fee, when another
/// player buys; unrecorded). Its prices are kept out of
/// every credit price's average and history, and the credit marketplace never sells one. What habbo.com was seen doing is
/// on habbo.com on 2026-11-03.</summary>
public sealed class MarketplaceService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    FurniDataLoader furniData,
    PlayerMessenger messenger,
    IClientVariables clientVariables,
    NftProducts nftProducts,
    IOptions<MarketplaceOptions> options,
    ILogger<MarketplaceService> logger) : IMarketplaceService
{
    private const string Reason = "marketplace";

    // the search's sort orders, as catalog.marketplace.sort.N names them
    private const int MostExpensive = 1;
    private const int Cheapest = 2;
    private const int MostTradedToday = 3;
    private const int LeastTradedToday = 4;
    private const int MostOffers = 5;
    private const int LeastOffers = 6;

    /// <summary>A price bound the search leaves open.</summary>
    private const int NoBound = -1;

    // the own offers' kinds, as September 2026 asks for them
    private const int OpenOffers = 1;
    private const int SoldOffers = 2;
    private const int ExpiredOffers = 3;

    /// <summary>The variable that tells the client how many items it may offer at once, and what the client takes when
    /// it is unset: the server holds to the same.</summary>
    private const string BulkOfferLimitVariable = "marketplace.bulkOfferLimit";
    private const int ClientBulkOfferLimit = 500;

    /// <summary>How many times a buyer of an NFT is offered the next cheapest when someone else bought the cheapest first.</summary>
    private const int NftBuyAttempts = 3;

    private MarketplaceOptions Rules => options.Value;

    private NftMarketplaceOptions Nft => options.Value.Nft;

    public Task SendConfigurationAsync(PlayerData player) =>
        messenger.SendAsync(player.Id, new MarketplaceConfigurationComposer(Rules.Enabled, Rules.Commission, Rules.TokenBatchPrice,
            Rules.TokenBatchSize, Rules.OfferMinPrice, Rules.OfferMaxPrice, Rules.ExpirationHours, Rules.AveragePricePeriodDays,
            Rules.SellingFeePercentage, Rules.RevenueLimit, Rules.HalfTaxLimit, Nft.Enabled, Nft.MinPrice, Nft.MaxPrice,
            Nft.OfferDurationMinutes, Nft.MaxOpenOffers, Nft.SellingFeePercentage, Nft.HalfTaxLimit));

    public async Task SendCanMakeOfferAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var tokens = await TokensAsync(db, player.Id);
        var result = player.TradeLocked ? MarketplaceCanMakeOfferResultComposer.TradingLocked
            : Rules.TokensRequired && tokens <= 0 ? MarketplaceCanMakeOfferResultComposer.NoTokens
            : MarketplaceCanMakeOfferResultComposer.Allowed;
        await messenger.SendAsync(player.Id, new MarketplaceCanMakeOfferResultComposer(result, tokens));
    }

    private static Task<int> TokensAsync(AppDbContext db, Guid playerId) =>
        db.MarketplaceTokens.Where(t => t.UserId == playerId).Select(t => t.Tokens).FirstOrDefaultAsync();

    public async Task BuyTokensAsync(PlayerData player)
    {
        // with tokens off there is no batch to sell
        if (!Rules.TokensRequired)
            return;

        var playerGrain = grains.GetGrain<IPlayerGrain>(player.Id);
        // spending tells the player their balance
        if (await playerGrain.SpendCreditsAsync(Rules.TokenBatchPrice, Reason) is null)
            return;

        // added to what the database holds, so batches bought or tokens spent at the same time all count
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var batch = Rules.TokenBatchSize;
            if (await AddTokensAsync(db, player.Id, batch) == 0)
            {
                db.MarketplaceTokens.Add(new MarketplaceTokens { UserId = player.Id, Tokens = batch });
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // another batch made the row first
                    db.ChangeTracker.Clear();
                    await AddTokensAsync(db, player.Id, batch);
                }
            }
        }

        await SendCanMakeOfferAsync(player);
    }

    private static Task<int> AddTokensAsync(AppDbContext db, Guid playerId, int tokens) =>
        db.MarketplaceTokens.Where(t => t.UserId == playerId).ExecuteUpdateAsync(s => s.SetProperty(t => t.Tokens, t => t.Tokens + tokens));

    /// <summary>What the seller receives of a credit price.</summary>
    private int RevenueOf(int price) => MarketplaceRules.RevenueOf(price, Rules.SellingFeePercentage, Rules.HalfTaxLimit);

    /// <summary>What the seller receives of an NFT's emerald price.</summary>
    private int NftRevenueOf(int price) => MarketplaceRules.RevenueOf(price, Nft.SellingFeePercentage, Nft.HalfTaxLimit);

    /// <summary>An NFT's kind as the NFT marketplace numbers it: 1 floor, 2 wall.</summary>
    private static int NftFurniTypeOf(ItemDefinition definition) =>
        definition.Type == ItemType.Wall ? MarketplaceOfferData.WallItem : MarketplaceOfferData.FloorItem;

    public async Task MakeOfferAsync(PlayerData player, int price, int furniType, IReadOnlyList<int> itemIds)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ids = itemIds.Distinct().ToList();
        var items = await db.UserItems.Include(i => i.Definition).Where(i => ids.Contains(i.Id) && i.OwnerId == player.Id
            && i.RoomId == null && !i.InMarketplace && i.ChestId == null && !db.UserItems.Any(stored => stored.ChestId == i.Id) && !db.ChestCoins.Any(c => c.ItemId == i.Id && c.Coins > 0) && i.Tradeable && i.Definition!.Tradeable)
            .ToListAsync();
        var expectedType = items.FirstOrDefault()?.Definition?.Type == ItemType.Wall ? MarketplaceOfferData.WallItem : MarketplaceOfferData.FloorItem;
        var tokens = await db.MarketplaceTokens.FirstOrDefaultAsync(t => t.UserId == player.Id);

        // several at once are of one kind, as the client offers them, each its own offer costing a token where tokens are on;
        // an NFT is sold on the NFT marketplace, never here
        if (!Rules.Enabled || player.TradeLocked || ids.Count == 0 || ids.Count != itemIds.Count || ids.Count > await BulkOfferLimitAsync()
            || items.Count != ids.Count || items.Select(i => i.DefinitionId).Distinct().Count() != 1 || furniType != expectedType
            || items.Any(i => ItemDefinition.IsNftClass(i.Definition!.ClassName))
            || (Rules.TokensRequired && (tokens is null || tokens.Tokens < items.Count))
            || price < Rules.OfferMinPrice || price > Rules.OfferMaxPrice || RevenueOf(price) > Rules.RevenueLimit)
        {
            await messenger.SendAsync(player.Id, new MarketplaceMakeOfferResultComposer(MarketplaceMakeOfferResultComposer.Failed));
            return;
        }

        // the tokens and the items are taken in the database, on the same terms they were checked on: another
        // offer, a trade or a recycling may have used them since
        await using var transaction = await db.Database.BeginTransactionAsync();
        var spent = Rules.TokensRequired
            ? await db.MarketplaceTokens.Where(t => t.UserId == player.Id && t.Tokens >= items.Count)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Tokens, t => t.Tokens - items.Count))
            : items.Count;
        var listed = await db.UserItems.Where(i => ids.Contains(i.Id) && i.OwnerId == player.Id && i.RoomId == null && !i.InMarketplace && i.ChestId == null
                && !db.UserItems.Any(stored => stored.ChestId == i.Id) && !db.ChestCoins.Any(c => c.ItemId == i.Id && c.Coins > 0))
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.InMarketplace, true));
        if (spent == 0 || listed != items.Count)
        {
            await transaction.RollbackAsync();
            await messenger.SendAsync(player.Id, new MarketplaceMakeOfferResultComposer(MarketplaceMakeOfferResultComposer.Failed));
            return;
        }

        var expiresAt = DateTimeOffset.UtcNow.AddHours(Rules.ExpirationHours);
        foreach (var item in items)
        {
            db.MarketplaceOffers.Add(new MarketplaceOffer
            {
                SellerId = player.Id, ItemId = item.Id, DefinitionId = item.DefinitionId, Price = price, Revenue = RevenueOf(price), ExpiresAt = expiresAt,
            });
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        await ForgetOpenOffersAsync();
        await messenger.SendAsync(player.Id, FurniListRemoveMultipleComposer.Items(items.Select(i => i.Id)));
        await messenger.SendAsync(player.Id, new MarketplaceMakeOfferResultComposer(MarketplaceMakeOfferResultComposer.Posted));
    }

    /// <summary>How many items may be offered at once: what the hotel's variables tell the client, or the client's own.</summary>
    private async Task<int> BulkOfferLimitAsync() =>
        int.TryParse(await clientVariables.GetAsync(BulkOfferLimitVariable), out var limit) && limit > 0 ? limit : ClientBulkOfferLimit;

    /// <remarks>The client neither locks nor hides the item while it waits (MarketplaceModel.startNftOfferMaking and
    /// makeNftOffer, where a credit offer locks its items with lockAllSellable), so a refusal has nothing on the screen to
    /// put back: the answer alone ends the offer (endOfferMaking alerts, and closing the alert releases it). A refusal is
    /// "Technical error occurred. Posting failed." (2) and nothing else, as habbo.com's (habbo.com, 2026-11-03); the NFT
    /// marketplace off is "Marketplace is disabled." (3), unrecorded: the official client never offers an NFT while it
    /// is off (MarketplaceModel.startNftOfferMaking), so no player sees this answer.</remarks>
    public async Task MakeNftOfferAsync(PlayerData player, int nftFurniType, int itemId, int price)
    {
        if (!Nft.Enabled)
        {
            await messenger.SendAsync(player.Id, new NftMarketplaceMakeOfferResultMessageComposer(NftMarketplaceMakeOfferResultMessageComposer.Disabled));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        // what the client offers is what it calls sellable; furnidata's tradeable is the credit marketplace's and trading's
        // rule, which many NFTs (moved through their own wallet) do not pass (unrecorded; habbo.com sells NFTs its furnidata marks untradeable)
        var item = await Sellable(db, player.Id).Include(i => i.Definition).FirstOrDefaultAsync(i => i.Id == itemId && i.Tradeable && i.Sellable);
        var open = Nft.MaxOpenOffers > 0
            ? await db.MarketplaceOffers.CountAsync(o => o.SellerId == player.Id && o.IsNft && o.SoldAt == null && o.ExpiresAt > now)
            : 0;
        if (player.TradeLocked || item?.Definition is not { } definition || !ItemDefinition.IsNftClass(definition.ClassName)
            || nftFurniType != NftFurniTypeOf(definition) || price < Nft.MinPrice || price > Nft.MaxPrice
            || (Nft.MaxOpenOffers > 0 && open >= Nft.MaxOpenOffers))
        {
            await messenger.SendAsync(player.Id, new NftMarketplaceMakeOfferResultMessageComposer(NftMarketplaceMakeOfferResultMessageComposer.Failed));
            return;
        }

        // taken in the database on the terms it was checked on, as a credit offer's items are
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (await Sellable(db, player.Id).Where(i => i.Id == itemId).ExecuteUpdateAsync(s => s.SetProperty(i => i.InMarketplace, true)) == 0)
        {
            await transaction.RollbackAsync();
            await messenger.SendAsync(player.Id, new NftMarketplaceMakeOfferResultMessageComposer(NftMarketplaceMakeOfferResultMessageComposer.Failed));
            return;
        }

        db.MarketplaceOffers.Add(new MarketplaceOffer
        {
            SellerId = player.Id, ItemId = item.Id, DefinitionId = item.DefinitionId, IsNft = true, Price = price, Revenue = NftRevenueOf(price),
            ExpiresAt = now.AddMinutes(Nft.OfferDurationMinutes),
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        // as a credit offer: the item leaves the inventory, then the answer
        await ForgetOpenOffersAsync();
        await messenger.SendAsync(player.Id, FurniListRemoveMultipleComposer.Items([item.Id]));
        await messenger.SendAsync(player.Id, new NftMarketplaceMakeOfferResultMessageComposer(NftMarketplaceMakeOfferResultMessageComposer.Posted));
    }

    /// <summary>The player's items that may go on sale: in their inventory, not on sale already, not in a chest, and no
    /// chest holding anything.</summary>
    private static IQueryable<UserItem> Sellable(AppDbContext db, Guid playerId) =>
        db.UserItems.Where(i => i.OwnerId == playerId && i.RoomId == null && !i.InMarketplace && i.ChestId == null
            && !db.UserItems.Any(stored => stored.ChestId == i.Id) && !db.ChestCoins.Any(c => c.ItemId == i.Id && c.Coins > 0));

    public async Task SendOffersAsync(PlayerData player, int minPrice, int maxPrice, string searchQuery, int sortOrder, bool combineUniques,
        int? offerSource = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var (all, openData) = await OpenOffersAsync(db, now);
        var products = await nftProducts.MapAsync();
        var search = searchQuery.Trim();
        var open = all.Where(o => o.ExpiresAt > now
                                  && MarketplaceRules.Lists(offerSource, o.IsNft, Nft.Enabled)
                                  && (minPrice == NoBound || o.Price >= minPrice)
                                  && (maxPrice == NoBound || o.Price <= maxPrice)
                                  && (search.Length == 0
                                      || o.Item!.Definition!.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                                      || o.Item!.Definition!.ClassName.Contains(search, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // one row per kind of item: limited editions per number unless combined, and a usable item used or not; an NFT one
        // row per product whatever its furni, apart from the same furni on sale for credits (as habbo.com,
        // 2026-11-03: one row of 49 offers at the cheapest one's price, combined or not)
        var groups = open.GroupBy(o => (DefinitionId: o.IsNft ? 0 : o.Item!.DefinitionId, Product: o.IsNft ? products.CodeOf(o.Item!.Definition!) : null,
            o.IsNft, Limited: combineUniques || o.IsNft ? null : o.Item!.UniqueSerialNumber,
            Used: o.IsNft ? null : Usage(openData[o.ItemId!.Value]))).ToList();
        var kindsOf = groups.ToDictionary(g => g.Key, g => g.Key.Product is { } product ? products.DefinitionsOf(product) : [g.Key.DefinitionId]);
        var classIds = kindsOf.Values.SelectMany(ids => ids).Distinct().ToList();
        var since = now.AddDays(-Rules.AveragePricePeriodDays);
        var today = now.Date;
        var sold = await db.MarketplaceOffers.AsNoTracking()
            .Where(o => o.SoldAt > since && classIds.Contains(o.DefinitionId))
            .Select(o => new { o.DefinitionId, o.IsNft, o.Price, o.SoldAt }).ToListAsync();
        var nftRowIds = MarketplaceRules.NftRowIds(groups.Where(g => g.Key.Product is not null).Select(g => (g.Key.Product!, g.Min(o => o.Id))));

        // a kind's sales in its own currency: credits for credit offers, emeralds for NFTs
        var cheapest = groups.Select(g =>
            {
                var ofKind = sold.Where(s => s.IsNft == g.Key.IsNft && kindsOf[g.Key].Contains(s.DefinitionId)).ToList();
                return (Offer: g.OrderBy(o => o.Price).ThenBy(o => o.Id).First(), Count: g.Count(), g.Key.Product,
                    SoldToday: ofKind.Count(s => s.SoldAt >= today), Average: AverageOf(ofKind.Select(s => s.Price)));
            })
            .ToList();
        cheapest = (sortOrder switch
        {
            MostExpensive => cheapest.OrderByDescending(c => c.Offer.Price),
            MostTradedToday => cheapest.OrderByDescending(c => c.SoldToday),
            LeastTradedToday => cheapest.OrderBy(c => c.SoldToday),
            MostOffers => cheapest.OrderByDescending(c => c.Count),
            LeastOffers => cheapest.OrderBy(c => c.Count),
            _ => cheapest.OrderBy(c => c.Offer.Price),
        }).ToList();

        var shown = cheapest.Take(Rules.ListLimit).ToList();
        await messenger.SendAsync(player.Id, new MarketPlaceOffersComposer(shown.Select(c =>
        {
            var look = Describe(c.Product, c.Offer.Item!, c.Offer.Item!.Definition!, openData[c.Offer.ItemId!.Value]);
            var rowId = c.Product is { } product ? nftRowIds[product] : c.Offer.Id;
            return new MarketplaceOfferData(rowId, MarketplaceOfferData.Open, MarketplaceOfferData.Legacy(look.Type), look.Type,
                look.ProductCode, c.Offer.Item!.Definition!.ClassId, look.NftFurniType, look.Stuff, look.Extra, look.Serial, look.Size,
                look.Used, c.Offer.Price, MinutesLeft(c.Offer, now), c.Average, c.Count);
        }).ToList(), cheapest.Count));
    }

    /// <summary>How long one load of the open marketplace answers searches. Every search reads all of it (offers are
    /// grouped by what their furni data says), so a client searching over and over reads it once per this.</summary>
    private static readonly TimeSpan OpenOffersFresh = TimeSpan.FromSeconds(3);

    private readonly SemaphoreSlim _openLock = new(1, 1);
    private (DateTimeOffset At, List<MarketplaceOffer> Offers, Dictionary<int, FurniDataView> Data)? _open;

    /// <summary>Every open offer with its item and the item's data, as loaded at most a moment ago.</summary>
    private async Task<(List<MarketplaceOffer> Offers, Dictionary<int, FurniDataView> Data)> OpenOffersAsync(AppDbContext db, DateTimeOffset now)
    {
        await _openLock.WaitAsync();
        try
        {
            if (_open is { } cached && now - cached.At < OpenOffersFresh)
                return (cached.Offers, cached.Data);

            var offers = await db.MarketplaceOffers.AsNoTracking().Include(o => o.Item!).ThenInclude(i => i.Definition)
                .Where(o => o.SoldAt == null && o.ExpiresAt > now && o.Item != null)
                .ToListAsync();
            var data = await furniData.LoadAsync(db, offers.Select(o => o.Item!).ToList());
            _open = (now, offers, data);
            return (offers, data);
        }
        finally
        {
            _openLock.Release();
        }
    }

    /// <summary>Drops the loaded open offers once some have changed, so the next search (the client asks again after a
    /// buy) shows the change rather than what was on sale a moment before.</summary>
    private async Task ForgetOpenOffersAsync()
    {
        await _openLock.WaitAsync();
        try
        {
            _open = null;
        }
        finally
        {
            _openLock.Release();
        }
    }

    private static int AverageOf(IEnumerable<int> prices)
    {
        var list = prices.ToList();
        return list.Count == 0 ? 0 : (int)Math.Round(list.Average());
    }

    private static int MinutesLeft(MarketplaceOffer offer, DateTimeOffset now) => (int)Math.Max(0, (offer.ExpiresAt - now).TotalMinutes);

    /// <summary>How the marketplace shows an item.</summary>
    private readonly record struct OfferLook(int Type, string? ProductCode = null, int? NftFurniType = null, FurniStuffData? Stuff = null,
        string? Extra = null, int? Serial = null, int? Size = null, bool? Used = null);

    /// <summary>How the marketplace shows an item: an NFT (one with a product code) by its product and whether it is a
    /// floor or a wall item, a floor item with its data, a wall item with its state string, a limited edition with its
    /// number, or a usable item with its data and whether it was used. A sold item no longer in the hotel is shown plain,
    /// as its kind.</summary>
    private static OfferLook Describe(string? productCode, UserItem? item, ItemDefinition definition, FurniDataView data) =>
        productCode is not null
            ? new OfferLook(MarketplaceOfferData.NftItem, productCode, NftFurniTypeOf(definition))
            : item?.UniqueSerialNumber is { } serial
                ? new OfferLook(MarketplaceOfferData.LimitedItem, Serial: serial, Size: item.UniqueSeriesSize ?? 0)
                : definition.Type == ItemType.Wall
                    ? new OfferLook(MarketplaceOfferData.WallItem, Extra: data.StateText)
                    : Usage(data) is { } used
                        ? new OfferLook(MarketplaceOfferData.UsableItem, Stuff: FurniStuffData.From(data), Used: used)
                        : new OfferLook(MarketplaceOfferData.FloorItem, Stuff: FurniStuffData.From(data));

    /// <summary>Whether a usable item has been used, or null for an item that is not used up: a crackable that takes more
    /// than one hit is usable, and used once it has been hit (the live hotel lists those, and only those, so).</summary>
    private static bool? Usage(FurniDataView data) =>
        data.Type == (int)FurniDataType.Crackable && data.Target > 1 ? data.Hits > 0 : null;

    public async Task SendOwnOffersAsync(PlayerData player, int? category = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var mine = db.MarketplaceOffers.AsNoTracking().Include(o => o.Item!).ThenInclude(i => i.Definition).Include(o => o.Definition)
            .Where(o => o.SellerId == player.Id);
        var listed = category switch
        {
            OpenOffers => mine.Where(o => o.SoldAt == null && o.ExpiresAt > now),
            SoldOffers => mine.Where(o => o.SoldAt != null && !o.ClearedBySeller),
            ExpiredOffers => mine.Where(o => o.SoldAt == null && o.ExpiresAt <= now),
            _ => mine.Where(o => !o.Redeemed),
        };
        var offers = await listed.OrderByDescending(o => o.Id).ToListAsync();
        var data = await furniData.LoadAsync(db, offers.Where(o => o.Item is not null).Select(o => o.Item!).ToList());
        var products = await nftProducts.MapAsync();

        // each kind's average price, as its stats give it: an NFT's in emeralds, apart from its furni's credit price
        var kinds = offers.Select(o => o.DefinitionId).Distinct().ToList();
        var since = now.AddDays(-Rules.AveragePricePeriodDays);
        var averages = (await db.MarketplaceOffers.AsNoTracking().Where(o => o.SoldAt > since && kinds.Contains(o.DefinitionId))
                .Select(o => new { o.DefinitionId, o.IsNft, o.Price }).ToListAsync())
            .GroupBy(o => (o.DefinitionId, o.IsNft)).ToDictionary(g => g.Key, g => AverageOf(g.Select(o => o.Price)));

        // what the credit sales not yet collected are worth, whichever kind is listed (an NFT's emeralds were paid as it sold)
        var waiting = await db.MarketplaceOffers.Where(o => o.SellerId == player.Id && o.SoldAt != null && !o.Redeemed && !o.IsNft).SumAsync(o => o.Revenue);
        await messenger.SendAsync(player.Id, new MarketPlaceOwnOffersComposer(waiting, offers.Take(Rules.ListLimit).Select(o =>
        {
            var look = Describe(o.IsNft ? products.CodeOf(o.Definition!) : null, o.Item, o.Definition!,
                o.ItemId is { } itemId && data.TryGetValue(itemId, out var view) ? view : FurniDataView.Plain);
            var status = o.IsSold ? MarketplaceOfferData.Sold : o.IsOpen(now) ? MarketplaceOfferData.Open : MarketplaceOfferData.Expired;
            var statusTime = status == MarketplaceOfferData.Sold ? o.SoldAt : status == MarketplaceOfferData.Expired ? o.ExpiresAt : null;
            return new MarketplaceOwnOfferData(o.Id, status, MarketplaceOfferData.Legacy(look.Type), look.Type, look.ProductCode,
                o.Definition!.ClassId, look.NftFurniType, look.Stuff, look.Extra, look.Serial, look.Size, look.Used, o.Price, MinutesLeft(o, now),
                averages.GetValueOrDefault((o.DefinitionId, o.IsNft)), statusTime?.ToUnixTimeMilliseconds());
        }).ToList()));
    }

    public async Task CancelAllAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var open = await db.MarketplaceOffers.Where(o => o.SellerId == player.Id && o.SoldAt == null && o.ExpiresAt > now).ToListAsync();
        var itemIds = await TakeBackAsync(db, open);
        if (itemIds.Count > 0)
            await grains.GetGrain<IPlayerGrain>(player.Id).AnnounceItemsAsync(itemIds);
        await messenger.SendAsync(player.Id, new MarketplaceCancelAllOffersResultComposer(open.Select(o => o.Id).ToList(), Success: true));
    }

    public async Task ClearHistoryAsync(PlayerData player, int category)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        switch (category)
        {
            case SoldOffers:
                // the sales leave the list; what they made is still the seller's to collect
                await db.MarketplaceOffers.Where(o => o.SellerId == player.Id && o.SoldAt != null && !o.ClearedBySeller)
                    .ExecuteUpdateAsync(s => s.SetProperty(o => o.ClearedBySeller, true));
                break;
            case ExpiredOffers:
                // an expired offer still holds its item, which comes back: nothing else would ever return it
                var expired = await db.MarketplaceOffers.Where(o => o.SellerId == player.Id && o.SoldAt == null && o.ExpiresAt <= now).ToListAsync();
                var itemIds = await TakeBackAsync(db, expired);
                if (itemIds.Count > 0)
                    await grains.GetGrain<IPlayerGrain>(player.Id).AnnounceItemsAsync(itemIds);
                break;
            default:
                await messenger.SendAsync(player.Id, new MarketplaceClearOwnHistoryResultComposer(Success: false));
                return;
        }

        await messenger.SendAsync(player.Id, new MarketplaceClearOwnHistoryResultComposer(Success: true));
    }

    /// <summary>Takes the offers off the marketplace, their items back to the seller's inventory; the ids of the items
    /// that came back. An offer sold since it was loaded stays: the sale is the buyer's, and its revenue the seller's.</summary>
    private async Task<List<int>> TakeBackAsync(AppDbContext db, List<MarketplaceOffer> offers)
    {
        // an offer goes only with its item coming back: one transaction for both, so neither happens alone
        var returned = new List<int>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        foreach (var offer in offers)
        {
            if (await db.MarketplaceOffers.Where(o => o.Id == offer.Id && o.SoldAt == null).ExecuteDeleteAsync() == 0)
                continue;

            if (offer.ItemId is not { } itemId)
                continue;

            await db.UserItems.Where(i => i.Id == itemId && i.OwnerId == offer.SellerId)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.InMarketplace, false));
            returned.Add(itemId);
        }

        await transaction.CommitAsync();
        if (returned.Count > 0)
            await ForgetOpenOffersAsync();
        return returned;
    }

    public async Task BuyAsync(PlayerData player, int offerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var offer = await db.MarketplaceOffers.Include(o => o.Item).FirstOrDefaultAsync(o => o.Id == offerId);
        // an NFT is bought for emeralds (BuyNftAsync), never for credits
        if (offer is null || !offer.IsOpen(now) || offer.SellerId == player.Id || offer.IsNft)
        {
            // gone: the same item on sale elsewhere is offered at its price instead
            var definitionId = offer?.DefinitionId;
            var other = definitionId is null ? null : await db.MarketplaceOffers.AsNoTracking()
                .Where(o => o.DefinitionId == definitionId && !o.IsNft && o.SoldAt == null && o.ExpiresAt > now && o.SellerId != player.Id && o.Id != offerId)
                .OrderBy(o => o.Price).FirstOrDefaultAsync();
            await messenger.SendAsync(player.Id, other is null
                ? new MarketplaceBuyOfferResultComposer(MarketplaceBuyOfferResultComposer.Gone, 0, 0, offerId)
                : new MarketplaceBuyOfferResultComposer(MarketplaceBuyOfferResultComposer.PriceChanged, other.Id, other.Price, offerId));
            return;
        }

        var buyer = grains.GetGrain<IPlayerGrain>(player.Id);
        if (await buyer.SpendCreditsAsync(offer.Price, Reason) is null)
        {
            await messenger.SendAsync(player.Id, new MarketplaceBuyOfferResultComposer(MarketplaceBuyOfferResultComposer.NotEnoughCredits, offerId, offer.Price, offerId));
            return;
        }

        // the sale is recorded only if nobody bought, cancelled or outlasted it first, and the item moves with it
        // only if it is still the seller's and on sale; otherwise neither happens and the price goes back
        bool sold;
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var taken = await db.MarketplaceOffers.Where(o => o.Id == offerId && !o.IsNft && o.SoldAt == null && o.ExpiresAt > now)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.SoldAt, now).SetProperty(o => o.BuyerId, player.Id));
            sold = taken > 0 && await db.UserItems.Where(i => i.Id == offer.ItemId && i.OwnerId == offer.SellerId && i.InMarketplace)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.OwnerId, player.Id).SetProperty(i => i.InMarketplace, false)) > 0;
            if (sold)
                await transaction.CommitAsync();
            else
                await transaction.RollbackAsync();
        }
        catch
        {
            // the price was taken before the sale: a sale that failed on the way gives it back, unless what failed was
            // only hearing that its commit landed
            if (!await db.MarketplaceOffers.AnyAsync(o => o.Id == offerId && o.BuyerId == player.Id && o.SoldAt != null))
                await buyer.AddCreditsAsync(offer.Price, Reason);
            throw;
        }

        if (!sold)
        {
            await messenger.SendAsync(player.Id, new CreditBalanceComposer(await buyer.AddCreditsAsync(offer.Price, Reason)));
            await messenger.SendAsync(player.Id, new MarketplaceBuyOfferResultComposer(MarketplaceBuyOfferResultComposer.Gone, 0, 0, offerId));
            return;
        }

        await ForgetOpenOffersAsync();
        await buyer.AnnounceItemsAsync([offer.ItemId!.Value]);
        await grains.GetGrain<IPlayerGrain>(offer.SellerId).ProgressAchievementAsync(AchievementNames.MarketplaceSales, 1);
        await messenger.SendAsync(player.Id, new MarketplaceBuyOfferResultComposer(MarketplaceBuyOfferResultComposer.Bought, offerId, offer.Price, offerId));
    }

    public async Task CancelAsync(PlayerData player, int offerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var offer = await db.MarketplaceOffers.AsNoTracking().FirstOrDefaultAsync(o => o.Id == offerId && o.SellerId == player.Id && o.SoldAt == null);
        if (offer is null || (await TakeBackAsync(db, [offer])).Count == 0)
        {
            await messenger.SendAsync(player.Id, new MarketplaceCancelOfferResultComposer(offerId, false));
            return;
        }

        // Habbo's order: the answer, then the item back
        await messenger.SendAsync(player.Id, new MarketplaceCancelOfferResultComposer(offerId, true));
        await grains.GetGrain<IPlayerGrain>(player.Id).RestoreItemsAsync([offer.ItemId!.Value]);
    }

    public async Task RedeemCreditsAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        // marked redeemed only where nobody marked them first: of two redeems at once, the one that finds
        // its rows already taken pays nothing
        await using var transaction = await db.Database.BeginTransactionAsync();
        var sold = await db.MarketplaceOffers.AsNoTracking().Where(o => o.SellerId == player.Id && o.SoldAt != null && !o.Redeemed && !o.IsNft)
            .Select(o => new { o.Id, o.Revenue }).ToListAsync();
        var ids = sold.Select(o => o.Id).ToList();
        if (sold.Count == 0
            || await db.MarketplaceOffers.Where(o => ids.Contains(o.Id) && !o.Redeemed).ExecuteUpdateAsync(s => s.SetProperty(o => o.Redeemed, true)) != sold.Count)
        {
            await transaction.RollbackAsync();
            return;
        }

        await transaction.CommitAsync();

        // a payment that fails may still have been made (a timeout says only that no answer came), so the offers stay
        // redeemed rather than risk paying twice; what was owed is logged
        var revenue = sold.Sum(o => o.Revenue);
        int balance;
        try
        {
            balance = await grains.GetGrain<IPlayerGrain>(player.Id).AddCreditsAsync(revenue, Reason);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Paying player {Player} {Credits} credits for marketplace offers {Offers} failed; they stay redeemed",
                player.Id, revenue, ids);
            throw;
        }
        await messenger.SendAsync(player.Id, new CreditBalanceComposer(balance));
        await SendOwnOffersAsync(player);
    }

    public async Task SendItemStatsAsync(PlayerData player, int furniType, int classId, string? posterId = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var itemType = furniType == MarketplaceOfferData.WallItem ? ItemType.Wall : ItemType.Floor;
        var definitionIds = await db.ItemDefinitions.AsNoTracking().Where(d => d.ClassId == classId && d.Type == itemType)
            .Select(d => d.Id).ToListAsync();

        // a poster's every design is its own item: one is told apart by its state, the poster id; NFT prices are emeralds
        var ofKind = db.MarketplaceOffers.AsNoTracking().Where(o => definitionIds.Contains(o.DefinitionId) && !o.IsNft);
        if (itemType == ItemType.Wall && !string.IsNullOrEmpty(posterId))
            ofKind = ofKind.Where(o => db.FurniStates.Any(s => s.ItemId == o.ItemId && s.State == posterId));

        var stats = await StatsAsync(ofKind, now, nftHistory: false);
        await messenger.SendAsync(player.Id, new MarketplaceItemStatsComposer(stats.Average, stats.OpenCount, Rules.StatsHistoryDays,
            stats.Days, furniType, classId, stats.Lowest, SuggestedPrice: stats.Lowest));
    }

    /// <remarks>Asked by product code, as habbo.com keys them: a code no NFT is listed under, a furni class name that is not
    /// its product's code among them, is answered with nothing on sale, no history, and the history's days (habbo.com,
    /// 2026-11-03: 0, 0, 30, 0, 0, 0 for nft_does_not_exist and for the class name nft_a0club_sofa). Off, nothing is
    /// answered: the client never asks then, since it lists no NFT to ask about and the hotel's rules are read once, at
    /// start (unrecorded).</remarks>
    public async Task SendNftItemStatsAsync(PlayerData player, string productCode)
    {
        if (!Nft.Enabled)
            return;

        var definitionIds = (await nftProducts.MapAsync()).DefinitionsOf(productCode);
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var ofKind = db.MarketplaceOffers.AsNoTracking().Where(o => o.IsNft && definitionIds.Contains(o.DefinitionId));
        var stats = await StatsAsync(ofKind, now, nftHistory: true);
        await messenger.SendAsync(player.Id, new NftMarketplaceItemStatsMessageComposer(stats.Average, stats.OpenCount, Rules.StatsHistoryDays,
            stats.Days, stats.Lowest, SuggestedPrice: stats.Lowest));
    }

    /// <summary>The sales of one kind of offer: the average over the average-price period, how many are on sale, each day
    /// of the history, and the lowest price on sale, which the live hotel also suggests (nothing when nothing is). An NFT's
    /// history runs day by day to yesterday (<see cref="MarketplaceRules.NftHistory"/>); a credit item's lists the days it
    /// sold on.</summary>
    private async Task<(int Average, int OpenCount, List<MarketplaceDayStatData> Days, int Lowest)> StatsAsync(
        IQueryable<MarketplaceOffer> ofKind, DateTimeOffset now, bool nftHistory)
    {
        var since = now.AddDays(-Rules.AveragePricePeriodDays);
        var historySince = now.AddDays(-Rules.StatsHistoryDays);
        var sold = await ofKind.Where(o => o.SoldAt > historySince).Select(o => new { o.Price, o.SoldAt }).ToListAsync();
        var openPrices = await ofKind.Where(o => o.SoldAt == null && o.ExpiresAt > now).Select(o => o.Price).ToListAsync();
        var lowest = openPrices.Count == 0 ? 0 : openPrices.Min();

        var days = nftHistory
            ? MarketplaceRules.NftHistory(sold.Select(s => (s.SoldAt!.Value, s.Price)), now, Rules.StatsHistoryDays)
            : sold.GroupBy(s => (int)(now.Date - s.SoldAt!.Value.Date).TotalDays)
                .OrderBy(g => g.Key)
                .Select(g => new MarketplaceDayStatData(-g.Key, AverageOf(g.Select(s => s.Price)), g.Count()))
                .ToList();
        return (AverageOf(sold.Where(s => s.SoldAt > since).Select(s => s.Price)), openPrices.Count, days, lowest);
    }

    /// <remarks>The client takes nothing off its screen before the answer (MarketplaceConfirmationDialog only sends and
    /// hides), and on anything but "SUCCESS", which has it ask for its list again, it only alerts (MarketPlaceLogic.onNftBuyResult).
    /// A refusal is the result with offer 0 and price 0 and nothing else, as habbo.com's (habbo.com, 2026-11-03): "UNKNOWN"
    /// for a product with nothing on sale, asked about before the balance; "NOT_ENOUGH_EMERALDS" for a buyer short of the
    /// cheapest offer's price, whatever price they named.</remarks>
    public async Task BuyNftAsync(PlayerData player, string productCode, int price)
    {
        // unrecorded: the NFT marketplace off is answered as an unknown product; the official client never buys while it is off,
        // and any answer but SUCCESS or NOT_ENOUGH_EMERALDS shows the same failure
        if (!Nft.Enabled)
        {
            await messenger.SendAsync(player.Id, NftMarketplaceBuyResultMessageComposer.Refused(NftMarketplaceBuyResultMessageComposer.Unknown));
            return;
        }

        var definitionIds = (await nftProducts.MapAsync()).DefinitionsOf(productCode);
        await using var db = await dbFactory.CreateDbContextAsync();
        var buyer = grains.GetGrain<IPlayerGrain>(player.Id);
        // someone else may buy the cheapest first: the next cheapest is bought instead, while it is still no dearer
        for (var attempt = 0; attempt < NftBuyAttempts; attempt++)
        {
            var now = DateTimeOffset.UtcNow;
            var offer = await db.MarketplaceOffers.AsNoTracking()
                .Where(o => o.IsNft && o.SoldAt == null && o.ExpiresAt > now && o.SellerId != player.Id && o.ItemId != null
                            && definitionIds.Contains(o.DefinitionId))
                .OrderBy(o => o.Price).ThenBy(o => o.Id).FirstOrDefaultAsync();
            if (offer is null)
                break;

            // "enough" is against the cheapest offer's price, the price paid: habbo.com was seen refusing with no emeralds at
            // both the listed price and below it, which does not tell the two apart
            if (await buyer.GetActivityPointsAsync(UserCurrency.Emeralds) < offer.Price)
            {
                await messenger.SendAsync(player.Id, NftMarketplaceBuyResultMessageComposer.Refused(NftMarketplaceBuyResultMessageComposer.NotEnoughEmeralds));
                return;
            }

            // the price sent only protects the buyer (habbo.com: "you'll always pay the cheapest price on offer"), so a dearer
            // cheapest is refused; its answer is unrecorded (habbo.com checks the emeralds before any price, habbo.com, 2026-11-03), and
            // "UNKNOWN" shows the same failure as any other
            if (offer.Price > price)
            {
                await messenger.SendAsync(player.Id, NftMarketplaceBuyResultMessageComposer.Refused(NftMarketplaceBuyResultMessageComposer.Unknown));
                return;
            }

            // the sale is taken first, inside a transaction, and the emeralds only then: a sale someone else won, or emeralds
            // gone meanwhile, is undone before any balance changes, so a refusal never sends one. Recorded only if nobody
            // bought, cancelled or outlasted it first, the item moving with it, and redeemed with it, since the seller is
            // paid now
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                var taken = await db.MarketplaceOffers.Where(o => o.Id == offer.Id && o.IsNft && o.SoldAt == null && o.ExpiresAt > now)
                    .ExecuteUpdateAsync(s => s.SetProperty(o => o.SoldAt, now).SetProperty(o => o.BuyerId, player.Id).SetProperty(o => o.Redeemed, true));
                if (taken == 0 || await db.UserItems.Where(i => i.Id == offer.ItemId && i.OwnerId == offer.SellerId && i.InMarketplace)
                        .ExecuteUpdateAsync(s => s.SetProperty(i => i.OwnerId, player.Id).SetProperty(i => i.InMarketplace, false)) == 0)
                {
                    await transaction.RollbackAsync();
                    continue;
                }

                // spending tells the buyer their new balance (habbo.com was seen sending it only at login; the balance has
                // changed, so it is sent, as credits are after a credit buy; unrecorded)
                if (!await buyer.SpendActivityPointsAsync(UserCurrency.Emeralds, offer.Price, Reason))
                {
                    await transaction.RollbackAsync();
                    await messenger.SendAsync(player.Id, NftMarketplaceBuyResultMessageComposer.Refused(NftMarketplaceBuyResultMessageComposer.NotEnoughEmeralds));
                    return;
                }

                try
                {
                    await transaction.CommitAsync();
                }
                catch
                {
                    // the emeralds were taken for a sale that did not land: given back unless what failed was only hearing
                    // that the commit landed
                    if (!await db.MarketplaceOffers.AnyAsync(o => o.Id == offer.Id && o.BuyerId == player.Id && o.SoldAt != null))
                        await buyer.AddActivityPointsAsync(UserCurrency.Emeralds, offer.Price, Reason);
                    throw;
                }
            }

            // as a credit sale: the item arrives in the buyer's inventory, the seller is paid (and told, if online), then the
            // answer, on which the client asks for its list again (as a credit sale, which habbo.com says this works "just like"; unrecorded, as we
            // could not buy one)
            await ForgetOpenOffersAsync();
            await buyer.AnnounceItemsAsync([offer.ItemId!.Value]);
            await PayNftSellerAsync(offer);
            await grains.GetGrain<IPlayerGrain>(offer.SellerId).ProgressAchievementAsync(AchievementNames.MarketplaceSales, 1);
            await messenger.SendAsync(player.Id, new NftMarketplaceBuyResultMessageComposer(NftMarketplaceBuyResultMessageComposer.Success, offer.Id, offer.Price));
            return;
        }

        await messenger.SendAsync(player.Id, NftMarketplaceBuyResultMessageComposer.Refused(NftMarketplaceBuyResultMessageComposer.Unknown));
    }

    /// <summary>Pays an NFT's seller its revenue in emeralds, which tells them if they are online. The sale is already
    /// redeemed: a payment that fails may still have been made, so it is logged rather than risk paying twice.</summary>
    private async Task PayNftSellerAsync(MarketplaceOffer offer)
    {
        try
        {
            await grains.GetGrain<IPlayerGrain>(offer.SellerId).AddActivityPointsAsync(UserCurrency.Emeralds, offer.Revenue, Reason);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Paying player {Player} {Emeralds} emeralds for NFT marketplace offer {Offer} failed; it stays redeemed",
                offer.SellerId, offer.Revenue, offer.Id);
        }
    }
}
