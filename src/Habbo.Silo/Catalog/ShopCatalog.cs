using Habbo.Domain.Catalog;
using Habbo.Domain.Furniture;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Catalog;

/// <summary>The shop as stored in the database. Reads the whole page table in one query and
/// assembles the tree in memory: the tree is small, and doing it recursively in SQL would cost
/// a round trip per level.</summary>
public sealed class ShopCatalog(IDbContextFactory<AppDbContext> dbFactory) : IShopCatalog
{
    public async Task<ShopPage?> GetTreeAsync(SecurityLevel rank, CancellationToken ct = default, bool buildersClub = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var pages = await db.CatalogPages
            .AsNoTracking()
            .Where(page => page.BuildersClub == buildersClub && page.MinRank <= rank && (page.ExpiresAt == null || page.ExpiresAt > DateTimeOffset.UtcNow))
            .OrderBy(page => page.OrderIndex)
            .ToListAsync(ct);

        if (pages.Count == 0)
            return null;

        // Offers are read through the link table, because one offer is listed on several pages
        // and its position differs on each.
        var listings = await db.CatalogListings
            .AsNoTracking()
            .OrderBy(listing => listing.OrderIndex)
            .Select(listing => new { listing.PageId, listing.OfferId })
            .ToListAsync(ct);

        var offersByPage = listings
            .GroupBy(listing => listing.PageId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<int>)group.Select(l => l.OfferId).ToList());

        var childrenByParent = pages
            .Where(page => page.ParentId is not null)
            .GroupBy(page => page.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        // A page whose parent was filtered out by rank would otherwise orphan its subtree, so
        // the root is the first page with no parent among those we kept.
        var root = pages.FirstOrDefault(page => page.ParentId is null);
        return root is null ? null : Build(root, childrenByParent, offersByPage);
    }

    public async Task<(ShopPage Page, IReadOnlyList<ShopOffer> Offers)?> GetPageAsync(
        int pageId, SecurityLevel rank, CancellationToken ct = default, bool buildersClub = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var page = await db.CatalogPages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pageId && p.BuildersClub == buildersClub && p.MinRank <= rank
                                      && (p.ExpiresAt == null || p.ExpiresAt > DateTimeOffset.UtcNow), ct);

        if (page is null)
            return null;

        var offers = await LoadOffersForPageAsync(db, pageId, ct);
        var ids = offers.Select(offer => offer.OfferId).ToList();

        return (ToPage(page, ids, []), offers);
    }

    public async Task<ShopOffer?> GetOfferAsync(int offerId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await LoadOfferAsync(db, offerId, ct);
    }

    public async Task<ShopOffer?> GetPurchasableOfferAsync(int offerId, SecurityLevel rank, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var purchasable = await db.CatalogListings.AnyAsync(listing => listing.OfferId == offerId && !listing.Page!.BuildersClub
                              && listing.Page.MinRank <= rank && (listing.Page.ExpiresAt == null || listing.Page.ExpiresAt > now), ct)
                          || await db.CatalogProducts.AnyAsync(product => product.OfferId == offerId && product.Type == CatalogProductType.Club, ct);
        return purchasable ? await LoadOfferAsync(db, offerId, ct) : null;
    }

    public async Task<ShopOffer?> GetBuildersClubOfferAsync(int pageId, int offerId, SecurityLevel rank, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var listed = await db.CatalogListings.AnyAsync(listing => listing.OfferId == offerId && listing.PageId == pageId
                                                                  && listing.Page!.BuildersClub && listing.Page.MinRank <= rank
                                                                  && (listing.Page.ExpiresAt == null || listing.Page.ExpiresAt > now), ct);
        return listed ? await LoadOfferAsync(db, offerId, ct) : null;
    }

    public async Task<IReadOnlyList<ShopFrontPageItem>> GetFrontPageItemsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = DateTimeOffset.UtcNow;
        return await db.CatalogFrontPageItems
            .AsNoTracking()
            .Where(item => item.ExpiresAt == null || item.ExpiresAt > now)
            .OrderBy(item => item.Position)
            .Select(item => new ShopFrontPageItem(item.Position, item.Name, item.PromoImage, item.Type,
                item.PageName, item.OfferId, item.ProductCode, item.ExpiresAt))
            .ToListAsync(ct);
    }

    /// <summary>A page's offers in the order Habbo listed them there. Ordering by offer id
    /// instead would shuffle every page into an order the client never shows.</summary>
    private static async Task<List<ShopOffer>> LoadOffersForPageAsync(
        AppDbContext db, int pageId, CancellationToken ct)
    {
        var listings = await db.CatalogListings
            .AsNoTracking()
            .Where(listing => listing.PageId == pageId)
            .OrderBy(listing => listing.OrderIndex)
            .Include(listing => listing.Offer!)
            .ThenInclude(offer => offer.Products)
            .ThenInclude(product => product.Definition)
            .ToListAsync(ct);

        // an offer not on sale yet is not shown; the next limited edition is announced instead
        return listings
            .Where(listing => listing.Offer is not null)
            .Select(listing => ToOffer(listing.Offer!))
            .Where(offer => offer.IsOnSale)
            .ToList();
    }

    private static async Task<ShopOffer?> LoadOfferAsync(AppDbContext db, int offerId, CancellationToken ct)
    {
        var offer = await db.CatalogOffers
            .AsNoTracking()
            .Include(o => o.Products)
            .ThenInclude(product => product.Definition)
            .FirstOrDefaultAsync(o => o.Id == offerId, ct);

        return offer is null ? null : ToOffer(offer);
    }

    private static ShopOffer ToOffer(CatalogOffer offer) => new(
        offer.Id,
        offer.LocalizationId,
        offer.PriceInCredits,
        offer.ClubLevel,
        offer.MaxAmount,
        offer.Products.Select(ToProduct).ToList(),
        offer.PriceInActivityPoints,
        (int)offer.ActivityPointType,
        offer.PriceInSilver,
        offer.IsPet,
        offer.Giftable,
        offer.IsRentable,
        offer.PreviewImage,
        offer.LimitedStack,
        offer.LimitedSold,
        offer.AvailableFrom,
        offer.ClubGiftDaysRequired > 0);

    private static ShopProduct ToProduct(CatalogProduct product) => new(
        product.DefinitionId,
        // The client renders furniture by its furnidata class id, not by our surrogate key.
        product.Definition?.ClassId ?? 0,
        product.Definition?.Type == ItemType.Wall,
        product.ExtraParam,
        product.Amount,
        (int)product.Type,
        product.ClassId,
        product.DurationSeconds);

    private static ShopPage Build(
        CatalogPage page,
        Dictionary<int, List<CatalogPage>> childrenByParent,
        Dictionary<int, IReadOnlyList<int>> offersByPage)
    {
        var children = childrenByParent.TryGetValue(page.Id, out var rows)
            ? rows.Select(child => Build(child, childrenByParent, offersByPage)).ToList()
            : [];

        return ToPage(page, offersByPage.GetValueOrDefault(page.Id, []), children);
    }

    private static ShopPage ToPage(
        CatalogPage page, IReadOnlyList<int> offerIds, IReadOnlyList<ShopPage> children) => new(
        page.Id, page.PageName, page.Localization, page.Icon, page.Visible, page.LayoutCode,
        page.MinRank, page.Images, page.Texts, offerIds, children);
}
