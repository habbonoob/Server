using Habbo.Domain.Catalog;
using Habbo.Domain.Subscriptions;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Subscriptions;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Subscriptions;

/// <summary>Habbo Club as the catalog sells it. A club offer is a catalog offer holding a club
/// product: the level, the months and any extra days. The monthly gifts are the offers on the
/// gifts page; a member earns one for every stretch of membership, and may take one whose days
/// they have reached.</summary>
public sealed class ClubService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    IShopCatalog catalog,
    PlayerMessenger messenger,
    IOptions<ClubOptions> options) : IClubService
{
    /// <summary>The client's club offers have a field it does not read.</summary>
    private const bool Unused = false;

    private const int Percent = 100;

    public async Task SendOffersAsync(PlayerData player, int source)
    {
        var (offers, daysLeft) = await ClubOffersAsync(player);
        await messenger.SendAsync(player.Id, new HabboClubOffersMessageComposer(
            offers.Select(offer => ToClubOffer(offer, daysLeft)).ToList(), source));
    }

    public async Task SendExtendOfferAsync(PlayerData player, bool vip)
    {
        if (await ExtendOfferAsync(player, vip) is not { } extend)
            return;

        var (offer, discounted, daysLeft) = extend;
        await messenger.SendAsync(player.Id, new HabboClubExtendOfferMessageComposer(new ClubOfferExtendData(
            ToClubOffer(discounted, daysLeft), offer.PriceInCredits, offer.PriceInActivityPoints, offer.ActivityPointType, daysLeft)));
    }

    public async Task<PurchaseResult> PurchaseExtensionAsync(PlayerData player, int offerId, bool vip)
    {
        // under a lock on the player's extensions until the purchase is made: two at once would otherwise both find
        // the membership short enough for the discount, and both take it
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await PlayerLock.TakeAsync(db, player.Id, nameof(PurchaseExtensionAsync));
        if (await ExtendOfferAsync(player, vip) is not { } extend || extend.Offer.OfferId != offerId)
            return PurchaseResult.Failed(PurchaseFailure.NotAllowed);

        var result = await grains.GetGrain<IPlayerGrain>(player.Id).PurchaseAsync(extend.Discounted, 1, string.Empty);
        await transaction.CommitAsync();
        return result;
    }

    /// <summary>The club offer extending the membership at a discount, while it has few enough days left.</summary>
    private async Task<(ShopOffer Offer, ShopOffer Discounted, int DaysLeft)?> ExtendOfferAsync(PlayerData player, bool vip)
    {
        var (offers, daysLeft) = await ClubOffersAsync(player);
        if (daysLeft <= 0 || daysLeft > options.Value.ExtendOfferDays)
            return null;

        var offer = offers.Where(o => IsVip(o) == vip).OrderBy(o => Club(o).Amount).FirstOrDefault();
        if (offer is null)
            return null;

        var keep = Percent - options.Value.ExtendDiscountPercent;
        return (offer, offer with
        {
            PriceInCredits = offer.PriceInCredits * keep / Percent,
            PriceInActivityPoints = offer.PriceInActivityPoints * keep / Percent,
        }, daysLeft);
    }

    /// <summary>Every club offer, and how many days the player's membership has left.</summary>
    private async Task<(List<ShopOffer> Offers, int DaysLeft)> ClubOffersAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ids = await db.CatalogProducts.AsNoTracking().Where(p => p.Type == CatalogProductType.Club)
            .Select(p => p.OfferId).Distinct().OrderBy(id => id).ToListAsync();
        var offers = new List<ShopOffer>();
        foreach (var id in ids)
            if (await catalog.GetOfferAsync(id) is { } offer)
                offers.Add(offer);

        var expires = await db.Subscriptions.AsNoTracking()
            .Where(s => s.UserId == player.Id && s.ProductName == SubscriptionProducts.HabboClub)
            .Select(s => (DateTimeOffset?)s.ExpiresAt).FirstOrDefaultAsync();
        var daysLeft = expires is { } end && end > DateTimeOffset.UtcNow ? (int)Math.Ceiling((end - DateTimeOffset.UtcNow).TotalDays) : 0;
        return (offers, daysLeft);
    }

    private static ShopProduct Club(ShopOffer offer) => offer.Products.First(p => p.Type == ProductTypes.Club);

    private static bool IsVip(ShopOffer offer) => Club(offer).ClassId == (int)ClubLevel.Vip;

    private ClubOfferData ToClubOffer(ShopOffer offer, int daysLeft)
    {
        var club = Club(offer);
        var extraDays = club.DurationSeconds / (int)TimeSpan.FromDays(1).TotalSeconds;
        var daysAfter = daysLeft + club.Amount * options.Value.DaysPerMonth + extraDays;
        var until = DateTimeOffset.UtcNow.AddDays(daysAfter);
        return new ClubOfferData(offer.OfferId, offer.LocalizationId, Unused, offer.PriceInCredits, offer.PriceInActivityPoints,
            offer.ActivityPointType, IsVip(offer), club.Amount, extraDays, offer.Giftable, daysAfter, until.Year, until.Month, until.Day);
    }

    /// <summary>The hotel's own payment page, when it has one, is configured; without one the answer is no.</summary>
    public Task SendDirectBuyAvailabilityAsync(PlayerData player, int lengthInDays) =>
        messenger.SendAsync(player.Id, new DirectSMSClubBuyAvailableMessageComposer(
            options.Value.DirectBuyPricePointUrl, options.Value.DirectBuyMarket, lengthInDays));

    public async Task SendGiftInfoAsync(PlayerData player)
    {
        var (gifts, memberDays, available, member) = await GiftsAsync(player);
        var interval = options.Value.GiftIntervalDays;
        var daysUntilNext = member ? interval - memberDays % interval : 0;
        // a gift is selectable when the member has the days for it and a gift is waiting: the live hotel marks none
        // selectable while none is
        await messenger.SendAsync(player.Id, new ClubGiftInfoComposer(daysUntilNext, available,
            gifts.Select(g => ToOfferData(g.Offer)).ToList(),
            gifts.Select(g => new ClubGiftData(g.Offer.OfferId, g.Offer.ClubLevel == ClubLevel.Vip, g.DaysRequired,
                memberDays >= g.DaysRequired && available > 0)).ToList()));
    }

    public async Task NotifyGiftsAsync(PlayerData player)
    {
        var (_, _, available, _) = await GiftsAsync(player);
        if (available > 0)
            await messenger.SendAsync(player.Id, new Habbo.Protocol.Messages.Outgoing.Notifications.ClubGiftNotificationComposer(available));
    }

    public async Task SelectGiftAsync(PlayerData player, string productCode)
    {
        // the claim is counted and recorded under a lock on the player's gifts, before the gift is handed over:
        // two selections at once would otherwise both find one gift waiting, and both take it
        await using var db = await dbFactory.CreateDbContextAsync();
        ClubGiftClaim claim;
        (List<(ShopOffer Offer, int DaysRequired)> Gifts, int, int, bool) state;
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await PlayerLock.TakeAsync(db, player.Id, nameof(SelectGiftAsync));
            state = await GiftsAsync(player);
            var (gifts, memberDays, available, _) = state;
            var index = gifts.FindIndex(g => g.Offer.LocalizationId == productCode);
            if (index < 0 || available <= 0 || memberDays < gifts[index].DaysRequired)
                return;

            db.ClubGiftClaims.Add(claim = new ClubGiftClaim { UserId = player.Id, OfferId = gifts[index].Offer.OfferId });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var gift = state.Gifts.First(g => g.Offer.OfferId == claim.OfferId);

        // the gift is free: taken as the offer with nothing to pay; the claim goes again if it could not be
        var free = gift.Offer with { PriceInCredits = 0, PriceInActivityPoints = 0, ClubLevel = ClubLevel.None, IsClubGift = false };
        var result = await grains.GetGrain<IPlayerGrain>(player.Id).PurchaseAsync(free, 1, string.Empty);
        if (!result.Success)
        {
            await db.ClubGiftClaims.Where(c => c.Id == claim.Id).ExecuteDeleteAsync();
            return;
        }

        await messenger.SendAsync(player.Id, new ClubGiftSelectedComposer(productCode, ToOfferData(gift.Offer).Products));
        await grains.GetGrain<IPlayerGrain>(player.Id).AnnounceItemsAsync(result.ItemIds ?? []);
    }

    /// <summary>The gifts page's offers with the days each needs, the days of membership the player has,
    /// how many gifts they have earned and not taken, and whether they are a member now.</summary>
    private async Task<(List<(ShopOffer Offer, int DaysRequired)> Gifts, int MemberDays, int Available, bool Member)> GiftsAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var page = await db.CatalogPages.AsNoTracking().FirstOrDefaultAsync(p => p.PageName == options.Value.GiftsPageName);
        var gifts = new List<(ShopOffer, int)>();
        if (page is not null)
        {
            var listed = await db.CatalogListings.AsNoTracking().Where(l => l.PageId == page.Id).OrderBy(l => l.OrderIndex)
                .Select(l => new { l.OfferId, l.Offer!.ClubGiftDaysRequired }).ToListAsync();
            foreach (var entry in listed)
                if (await catalog.GetOfferAsync(entry.OfferId) is { } offer)
                    gifts.Add((offer, entry.ClubGiftDaysRequired));
        }

        var subscription = await db.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == player.Id && s.ProductName == SubscriptionProducts.HabboClub);
        var now = DateTimeOffset.UtcNow;
        var memberDays = subscription is null
            ? 0
            : subscription.PastVipDays + (subscription.IsActive(now) && subscription.StreakStartedAt is { } started ? (int)(now - started).TotalDays : 0);
        var claimed = await db.ClubGiftClaims.CountAsync(c => c.UserId == player.Id);
        var member = subscription is { } active && active.IsActive(now);
        var earned = member ? memberDays / options.Value.GiftIntervalDays : 0;
        return (gifts, memberDays, Math.Max(0, earned - claimed), member);
    }

    private static CatalogOfferData ToOfferData(ShopOffer offer) => CatalogOfferDataFactory.From(offer);
}
