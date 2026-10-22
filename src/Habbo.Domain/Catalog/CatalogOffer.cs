using Habbo.Domain.Users;

namespace Habbo.Domain.Catalog;

/// <summary>One purchasable thing. The price lives here rather than on the item definition,
/// because an offer may hand over several products for a single price — that is exactly what a
/// bundle is, and the client reads offers, not items.
///
/// An offer belongs to any number of pages: Habbo lists the same item under its themed set,
/// its category page and sometimes a game page, all sharing one offer id. That id is what the
/// client sends back to buy, so the offer is stored once and linked to each page it appears
/// on.</summary>
public sealed class CatalogOffer
{
    /// <summary>The wire offer id the client sends back in PurchaseFromCatalog.</summary>
    public int Id { get; set; }

    /// <summary>Every page this offer is listed on.</summary>
    public List<CatalogListing> Listings { get; } = [];

    /// <summary>The external-texts key for the offer's name; conventionally the class name
    /// of its first product, which is how the client finds the furniture's own label.</summary>
    public required string LocalizationId { get; set; }

    public int PriceInCredits { get; set; }

    /// <summary>The price in a non-credit currency, and which one. Habbo sells whole pages in
    /// duckets or diamonds, so an offer that only carries a credit price of 0 is not free —
    /// it is priced in something we failed to record.</summary>
    public int PriceInActivityPoints { get; set; }
    public ActivityPointType ActivityPointType { get; set; } = ActivityPointType.Ducket;

    /// <summary>Silver is counted separately from the activity-point currencies.</summary>
    public int PriceInSilver { get; set; }

    /// <summary>True when the offer sells a pet rather than furniture; the client reads this
    /// to decide how to render the tile.</summary>
    public bool IsPet { get; set; }

    /// <summary>Whether the offer can be sent to another player as a gift.</summary>
    public bool Giftable { get; set; }

    /// <summary>Set when the offer is rented rather than bought outright.</summary>
    public bool IsRentable { get; set; }

    /// <summary>The image the client shows in the offer's preview, when it has one.</summary>
    public string PreviewImage { get; set; } = string.Empty;

    /// <summary>Habbo Club level required to buy; 0 is everyone.</summary>
    public ClubLevel ClubLevel { get; set; }

    /// <summary>The quantity spinner's ceiling for this offer.</summary>
    public int MaxAmount { get; set; } = 100;

    public List<CatalogProduct> Products { get; } = [];

    /// <summary>A limited edition's run: how many there are, none for an ordinary offer.</summary>
    public int LimitedStack { get; set; }

    /// <summary>How many of the run are sold; the next one sold carries the next serial number.</summary>
    public int LimitedSold { get; set; }

    public bool IsLimited => LimitedStack > 0;

    /// <summary>When the offer goes on sale, if not yet: a limited edition is announced before it is.</summary>
    public DateTimeOffset? AvailableFrom { get; set; }

    /// <summary>For one of the club's monthly gifts: the days of membership it needs.</summary>
    public int ClubGiftDaysRequired { get; set; }
}
