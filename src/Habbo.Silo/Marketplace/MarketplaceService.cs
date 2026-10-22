using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Marketplace;
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
/// expires after its hours and its owner may take the item back.</summary>
public sealed class MarketplaceService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    FurniDataLoader furniData,
    PlayerMessenger messenger,
    IClientVariables clientVariables,
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

    private MarketplaceOptions Rules => options.Value;

    public Task SendConfigurationAsync(PlayerData player) =>
        messenger.SendAsync(player.Id, new MarketplaceConfigurationComposer(Rules.Enabled, Rules.Commission, Rules.TokenBatchPrice,
            Rules.TokenBatchSize, Rules.OfferMinPrice, Rules.OfferMaxPrice, Rules.ExpirationHours, Rules.AveragePricePeriodDays,
            Rules.SellingFeePercentage, Rules.RevenueLimit, Rules.HalfTaxLimit));

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

    /// <summary>What the seller receives of a price: the price less the selling fee, which grows with the price.</summary>
    private int RevenueOf(int price)
    {
        var fee = Math.Ceiling(Math.Round(1000 * (price * (Rules.SellingFeePercentage / 100.0 + 0.5 * price / Rules.HalfTaxLimit))) / 1000);
        return price - (int)fee;
    }

    public async Task MakeOfferAsync(PlayerData player, int price, int furniType, IReadOnlyList<int> itemIds)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ids = itemIds.Distinct().ToList();
        var items = await db.UserItems.Include(i => i.Definition).Where(i => ids.Contains(i.Id) && i.OwnerId == player.Id
            && i.RoomId == null && !i.InMarketplace && i.ChestId == null && !db.UserItems.Any(stored => stored.ChestId == i.Id) && !db.ChestCoins.Any(c => c.ItemId == i.Id && c.Coins > 0) && i.Tradeable && i.Definition!.Tradeable)
            .ToListAsync();
        var expectedType = items.FirstOrDefault()?.Definition?.Type == ItemType.Wall ? MarketplaceOfferData.WallItem : MarketplaceOfferData.FloorItem;
        var tokens = await db.MarketplaceTokens.FirstOrDefaultAsync(t => t.UserId == player.Id);

        // several at once are of one kind, as the client offers them, each its own offer costing a token where tokens are on
        if (!Rules.Enabled || player.TradeLocked || ids.Count == 0 || ids.Count != itemIds.Count || ids.Count > await BulkOfferLimitAsync()
            || items.Count != ids.Count || items.Select(i => i.DefinitionId).Distinct().Count() != 1 || furniType != expectedType
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

        await messenger.SendAsync(player.Id, FurniListRemoveMultipleComposer.Items(items.Select(i => i.Id)));
        await messenger.SendAsync(player.Id, new MarketplaceMakeOfferResultComposer(MarketplaceMakeOfferResultComposer.Posted));
    }

    /// <summary>How many items may be offered at once: what the hotel's variables tell the client, or the client's own.</summary>
    private async Task<int> BulkOfferLimitAsync() =>
        int.TryParse(await clientVariables.GetAsync(BulkOfferLimitVariable), out var limit) && limit > 0 ? limit : ClientBulkOfferLimit;

    public async Task SendOffersAsync(PlayerData player, int minPrice, int maxPrice, string searchQuery, int sortOrder, bool combineUniques)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var (all, openData) = await OpenOffersAsync(db, now);
        var search = searchQuery.Trim();
        var open = all.Where(o => o.ExpiresAt > now
                                  && (minPrice == NoBound || o.Price >= minPrice)
                                  && (maxPrice == NoBound || o.Price <= maxPrice)
                                  && (search.Length == 0
                                      || o.Item!.Definition!.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                                      || o.Item!.Definition!.ClassName.Contains(search, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // one row per kind of item: limited editions per number unless combined, and a usable item used or not
        var groups = open.GroupBy(o => (o.Item!.DefinitionId, Limited: combineUniques ? null : o.Item.UniqueSerialNumber,
            Used: Usage(openData[o.ItemId!.Value]))).ToList();
        var classIds = groups.Select(g => g.Key.DefinitionId).Distinct().ToList();
        var since = now.AddDays(-Rules.AveragePricePeriodDays);
        var today = now.Date;
        var sold = await db.MarketplaceOffers.AsNoTracking()
            .Where(o => o.SoldAt > since && classIds.Contains(o.DefinitionId))
            .Select(o => new { o.DefinitionId, o.Price, o.SoldAt }).ToListAsync();

        var cheapest = groups.Select(g => (Offer: g.OrderBy(o => o.Price).ThenBy(o => o.Id).First(), Count: g.Count(),
                SoldToday: sold.Count(s => s.DefinitionId == g.Key.DefinitionId && s.SoldAt >= today),
                Average: AverageOf(sold.Where(s => s.DefinitionId == g.Key.DefinitionId).Select(s => s.Price))))
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
            var (type, stuff, extra, serial, size, used) = Describe(c.Offer.Item!, c.Offer.Item!.Definition!, openData[c.Offer.ItemId!.Value]);
            return new MarketplaceOfferData(c.Offer.Id, MarketplaceOfferData.Open, MarketplaceOfferData.Legacy(type), type,
                c.Offer.Item!.Definition!.ClassId, stuff, extra, serial, size, used, c.Offer.Price, MinutesLeft(c.Offer, now),
                c.Average, c.Count);
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

    private static int AverageOf(IEnumerable<int> prices)
    {
        var list = prices.ToList();
        return list.Count == 0 ? 0 : (int)Math.Round(list.Average());
    }

    private static int MinutesLeft(MarketplaceOffer offer, DateTimeOffset now) => (int)Math.Max(0, (offer.ExpiresAt - now).TotalMinutes);

    /// <summary>How the marketplace shows an item: a floor item with its data, a wall item with its
    /// state string, a limited edition with its number, or a usable item with its data and whether it was used.
    /// A sold item no longer in the hotel is shown plain, as its kind.</summary>
    private static (int Type, FurniStuffData? Stuff, string? Extra, int? Serial, int? Size, bool? Used) Describe(
        UserItem? item, ItemDefinition definition, FurniDataView data) =>
        item?.UniqueSerialNumber is { } serial
            ? (MarketplaceOfferData.LimitedItem, null, null, serial, item.UniqueSeriesSize ?? 0, null)
            : definition.Type == ItemType.Wall
                ? (MarketplaceOfferData.WallItem, null, data.StateText, null, null, null)
                : Usage(data) is { } used
                    ? (MarketplaceOfferData.UsableItem, FurniStuffData.From(data), null, null, null, used)
                    : (MarketplaceOfferData.FloorItem, FurniStuffData.From(data), null, null, null, null);

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

        // each kind's average price, as its stats give it
        var kinds = offers.Select(o => o.DefinitionId).Distinct().ToList();
        var since = now.AddDays(-Rules.AveragePricePeriodDays);
        var averages = (await db.MarketplaceOffers.AsNoTracking().Where(o => o.SoldAt > since && kinds.Contains(o.DefinitionId))
                .Select(o => new { o.DefinitionId, o.Price }).ToListAsync())
            .GroupBy(o => o.DefinitionId).ToDictionary(g => g.Key, g => AverageOf(g.Select(o => o.Price)));

        // what the sales not yet collected are worth, whichever kind is listed
        var waiting = await db.MarketplaceOffers.Where(o => o.SellerId == player.Id && o.SoldAt != null && !o.Redeemed).SumAsync(o => o.Revenue);
        await messenger.SendAsync(player.Id, new MarketPlaceOwnOffersComposer(waiting, offers.Take(Rules.ListLimit).Select(o =>
        {
            var (type, stuff, extra, serial, size, used) = Describe(o.Item, o.Definition!,
                o.ItemId is { } itemId && data.TryGetValue(itemId, out var view) ? view : FurniDataView.Plain);
            var status = o.IsSold ? MarketplaceOfferData.Sold : o.IsOpen(now) ? MarketplaceOfferData.Open : MarketplaceOfferData.Expired;
            var statusTime = status == MarketplaceOfferData.Sold ? o.SoldAt : status == MarketplaceOfferData.Expired ? o.ExpiresAt : null;
            return new MarketplaceOwnOfferData(o.Id, status, MarketplaceOfferData.Legacy(type), type, o.Definition!.ClassId,
                stuff, extra, serial, size, used, o.Price, MinutesLeft(o, now), averages.GetValueOrDefault(o.DefinitionId), statusTime?.ToUnixTimeMilliseconds());
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
    private static async Task<List<int>> TakeBackAsync(AppDbContext db, List<MarketplaceOffer> offers)
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
        return returned;
    }

    public async Task BuyAsync(PlayerData player, int offerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var offer = await db.MarketplaceOffers.Include(o => o.Item).FirstOrDefaultAsync(o => o.Id == offerId);
        if (offer is null || !offer.IsOpen(now) || offer.SellerId == player.Id)
        {
            // gone: the same item on sale elsewhere is offered at its price instead
            var definitionId = offer?.DefinitionId;
            var other = definitionId is null ? null : await db.MarketplaceOffers.AsNoTracking()
                .Where(o => o.DefinitionId == definitionId && o.SoldAt == null && o.ExpiresAt > now && o.SellerId != player.Id && o.Id != offerId)
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
            var taken = await db.MarketplaceOffers.Where(o => o.Id == offerId && o.SoldAt == null && o.ExpiresAt > now)
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
        var sold = await db.MarketplaceOffers.AsNoTracking().Where(o => o.SellerId == player.Id && o.SoldAt != null && !o.Redeemed)
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
        var since = now.AddDays(-Rules.AveragePricePeriodDays);
        var historySince = now.AddDays(-Rules.StatsHistoryDays);

        // a poster's every design is its own item: one is told apart by its state, the poster id
        var ofKind = db.MarketplaceOffers.AsNoTracking().Where(o => definitionIds.Contains(o.DefinitionId));
        if (itemType == ItemType.Wall && !string.IsNullOrEmpty(posterId))
            ofKind = ofKind.Where(o => db.FurniStates.Any(s => s.ItemId == o.ItemId && s.State == posterId));

        var sold = await ofKind.Where(o => o.SoldAt > historySince).Select(o => new { o.Price, o.SoldAt }).ToListAsync();
        var openPrices = await ofKind.Where(o => o.SoldAt == null && o.ExpiresAt > now).Select(o => o.Price).ToListAsync();
        var openCount = openPrices.Count;

        // what to ask: the live hotel suggests the lowest price on sale, and nothing when nothing is
        var lowest = openPrices.Count == 0 ? 0 : openPrices.Min();

        var days = sold.GroupBy(s => (int)(now.Date - s.SoldAt!.Value.Date).TotalDays)
            .OrderBy(g => g.Key)
            .Select(g => new MarketplaceDayStatData(-g.Key, AverageOf(g.Select(s => s.Price)), g.Count()))
            .ToList();
        await messenger.SendAsync(player.Id, new MarketplaceItemStatsComposer(AverageOf(sold.Where(s => s.SoldAt > since).Select(s => s.Price)),
            openCount, Rules.StatsHistoryDays, days, furniType, classId, lowest, SuggestedPrice: lowest));
    }
}
