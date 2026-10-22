using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Catalog;

/// <summary>What the catalog shows around its pages.</summary>
public sealed class CatalogExtrasService(
    IDbContextFactory<AppDbContext> dbFactory,
    IShopCatalog catalog,
    PlayerMessenger messenger,
    IOptions<CatalogExtrasOptions> options) : ICatalogExtrasService
{
    /// <summary>What the next-limited-edition answer says when none is coming.</summary>
    private const int NoneComing = -1;

    public async Task SendPageExpirationAsync(PlayerData player, string pageName)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var page = await db.CatalogPages.AsNoTracking().FirstOrDefaultAsync(p => p.PageName == pageName && p.ExpiresAt > now);
        if (page is not null)
            await messenger.SendAsync(player.Id, new CatalogPageExpirationComposer(page.Id, page.PageName,
                (int)(page.ExpiresAt!.Value - now).TotalSeconds, page.ExpiryImage));
    }

    public async Task SendEarliestExpiringPageAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var page = await db.CatalogPages.AsNoTracking().Where(p => p.ExpiresAt > now && p.Visible && p.MinRank <= player.Rank)
            .OrderBy(p => p.ExpiresAt).FirstOrDefaultAsync();
        if (page is not null)
            await messenger.SendAsync(player.Id, new CatalogPageWithEarliestExpiryMessageComposer(page.PageName,
                (int)(page.ExpiresAt!.Value - now).TotalSeconds, page.ExpiryImage));
    }

    public async Task SendLimitedOfferAppearingNextAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var next = await db.CatalogListings.AsNoTracking()
            .Where(l => l.Offer!.LimitedStack > 0 && l.Offer.AvailableFrom > now)
            .OrderBy(l => l.Offer!.AvailableFrom)
            .Select(l => new { l.PageId, l.OfferId, l.Offer!.AvailableFrom })
            .FirstOrDefaultAsync();
        if (next is null)
        {
            await messenger.SendAsync(player.Id, new LimitedOfferAppearingNextMessageComposer(NoneComing, NoneComing, NoneComing, string.Empty));
            return;
        }

        var offer = await catalog.GetOfferAsync(next.OfferId);
        var product = offer?.Products.FirstOrDefault();
        await messenger.SendAsync(player.Id, new LimitedOfferAppearingNextMessageComposer(
            (int)(next.AvailableFrom!.Value - now).TotalSeconds, next.PageId, next.OfferId,
            product is { IsWallItem: true } ? CatalogProductData.WallType : CatalogProductData.FloorType));
    }

    public async Task SendBonusRareInfoAsync(PlayerData player)
    {
        var bonus = options.Value;
        await using var db = await dbFactory.CreateDbContextAsync();
        var rare = await db.ItemDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.ClassName == bonus.BonusRareClassName);
        if (rare is null || bonus.BonusRareCredits <= 0)
            return;

        var spent = await db.BonusRareProgress.Where(p => p.UserId == player.Id).Select(p => p.CreditsSpent).FirstOrDefaultAsync();
        await messenger.SendAsync(player.Id, new BonusRareInfoMessageComposer(
            rare.Type == Habbo.Domain.Furniture.ItemType.Wall ? CatalogProductData.WallType : CatalogProductData.FloorType,
            rare.ClassId, bonus.BonusRareCredits, Math.Max(0, bonus.BonusRareCredits - spent)));
    }

    public async Task SendSeasonalCalendarOfferAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var page = await db.CatalogPages.AsNoTracking().FirstOrDefaultAsync(p => p.PageName == options.Value.SeasonalCalendarPage);
        if (page is null)
            return;

        var day = DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - options.Value.SeasonalCalendarStart.DayNumber;
        var offerIds = await db.CatalogListings.AsNoTracking().Where(l => l.PageId == page.Id).OrderBy(l => l.OrderIndex)
            .Select(l => l.OfferId).ToListAsync();
        if (day < 0 || day >= offerIds.Count || await catalog.GetOfferAsync(offerIds[day]) is not { } offer)
            return;

        await messenger.SendAsync(player.Id, new SeasonalCalendarDailyOfferMessageComposer(page.Id, CatalogOfferDataFactory.From(offer)));
    }
}
