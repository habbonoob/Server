using Habbo.Domain.Users;

namespace Habbo.Domain.Catalog;

/// <summary>One page of the catalog tree. Pages nest through <see cref="ParentId"/>: the client
/// renders the tree in its left-hand column and asks for a page's offers only when it is opened.
/// A page with no layout is a folder — it holds children but sells nothing.</summary>
public sealed class CatalogPage
{
    /// <summary>The wire page id the client sends back in GetCatalogPage.</summary>
    public int Id { get; set; }

    /// <summary>Null for a root-level page.</summary>
    public int? ParentId { get; set; }
    public CatalogPage? Parent { get; set; }
    public List<CatalogPage> Children { get; } = [];

    /// <summary>The page's internal name; the client uses it for deep links.</summary>
    public required string PageName { get; set; }

    /// <summary>The external-texts key the client renders as the page's title.</summary>
    public required string Localization { get; set; }

    /// <summary>The index into the client's catalog icon sprite sheet.</summary>
    public int Icon { get; set; }

    /// <summary>Hidden pages stay out of the tree but can still be opened by id.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>The client-side template that lays the offers out, e.g. "default_3x3".</summary>
    public string LayoutCode { get; set; } = "default_3x3";

    /// <summary>Images and text blocks the layout indexes positionally.</summary>
    public List<string> Images { get; set; } = [];
    public List<string> Texts { get; set; } = [];

    /// <summary>Lowest rank that may see the page, so staff pages can be hidden.</summary>
    public SecurityLevel MinRank { get; set; } = SecurityLevel.AboveNone;

    /// <summary>Order among siblings.</summary>
    public int OrderIndex { get; set; }

    /// <summary>A page of the Builders Club warehouse rather than the shop: the client asks for its tree and pages
    /// in the BUILDERS_CLUB mode, and its offers are borrowed and placed, never bought.</summary>
    public bool BuildersClub { get; set; }

    /// <summary>When the page stops being sold, if it does; the picture its countdown shows.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    public string ExpiryImage { get; set; } = string.Empty;

    /// <summary>The offers listed on this page, each with its position. The same offer may be
    /// listed on other pages, at a different position on each.</summary>
    public List<CatalogListing> Listings { get; } = [];
}
