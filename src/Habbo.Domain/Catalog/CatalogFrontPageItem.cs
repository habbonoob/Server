namespace Habbo.Domain.Catalog;

/// <summary>One promotion on the catalog's front page: a picture and a caption in one of its slots, which opens a
/// page, an offer or a product when clicked, until it expires.</summary>
public sealed class CatalogFrontPageItem
{
    public int Id { get; set; }

    /// <summary>The slot it fills, from 1: the front page layout places each by number.</summary>
    public int Position { get; set; }

    /// <summary>The caption, e.g. "NEW: Dark Academia Furni!".</summary>
    public required string Name { get; set; }

    /// <summary>The picture, under the catalogue image library, e.g. "catalogue/feature_cata_hort_fx_oct26.png".</summary>
    public required string PromoImage { get; set; }

    public CatalogFrontPageItemType Type { get; set; }

    /// <summary>What it opens: a page name, an offer id or a product code, as <see cref="Type"/> says.</summary>
    public string PageName { get; set; } = string.Empty;
    public int OfferId { get; set; }
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>When it comes down; null for never.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
