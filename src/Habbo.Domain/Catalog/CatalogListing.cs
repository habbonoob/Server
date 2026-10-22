namespace Habbo.Domain.Catalog;

/// <summary>One offer's place on one page.
///
/// This is a table of its own rather than a plain link, because the position is a property of
/// the listing and not of either side: the same offer is listed on several pages and sits in a
/// different place on each. Ordering by offer id instead would shuffle every page into an order
/// Habbo never used.</summary>
public sealed class CatalogListing
{
    public int PageId { get; set; }
    public CatalogPage? Page { get; set; }

    public int OfferId { get; set; }
    public CatalogOffer? Offer { get; set; }

    /// <summary>Position within the page, in the order Habbo sent the offers.</summary>
    public int OrderIndex { get; set; }
}
