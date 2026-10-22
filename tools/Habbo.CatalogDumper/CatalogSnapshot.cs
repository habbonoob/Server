using Xabbo.Core;

namespace Habbo.CatalogDumper;

/// <summary>One catalog page as we captured it: the tree node says where it sits and what it is
/// called, the page response says how it is laid out and what it sells. Neither half is enough
/// on its own, which is why both are kept together.</summary>
/// <param name="Node">The index-tree entry.</param>
/// <param name="Page">The page detail, or null when the page could not be fetched.</param>
public sealed record CapturedPage(ICatalogPageNode Node, ICatalogPage? Page)
{
    /// <summary>The id this page is stored under. Usually the node's own id, but Habbo marks
    /// every folder node with -1 — they are not real pages and cannot be requested — so folders
    /// are given a synthetic id here instead. Without that they would all collide on -1.</summary>
    public required int Id { get; init; }

    /// <summary>True when Habbo gave this node no page id of its own: it groups pages and
    /// sells nothing, so no page was ever requested for it.</summary>
    public bool IsFolder { get; init; }

    /// <summary>The page's parent id, or null at the root.</summary>
    public int? ParentId { get; init; }

    /// <summary>Position among its siblings, so the tree renders in the order Habbo sent it.</summary>
    public int OrderIndex { get; init; }

    /// <summary>A node with no layout is a folder: it groups pages but sells nothing.</summary>
    public string LayoutCode => Page?.LayoutCode ?? string.Empty;

    public IReadOnlyList<string> Images => Page?.Images ?? [];
    public IReadOnlyList<string> Texts => Page?.Texts ?? [];
    public IReadOnlyList<ICatalogOffer> Offers => Page?.Offers ?? [];
}

/// <summary>Everything captured in one run.</summary>
/// <param name="CatalogType">The catalog that was walked, e.g. "NORMAL".</param>
/// <param name="Pages">Pages in tree order, parents before children.</param>
public sealed record CatalogSnapshot(string CatalogType, IReadOnlyList<CapturedPage> Pages)
{
    /// <summary>Listings: one offer on one page. Higher than the offer count, because Habbo
    /// lists the same offer on several pages. Counted as distinct pairs, because a page can
    /// also name the same offer twice and only one row is written for it.</summary>
    public int OfferCount => Pages
        .SelectMany(page => page.Offers.Select(offer => (page.Id, offer.Id)))
        .Distinct()
        .Count();

    public int DistinctOfferCount =>
        Pages.SelectMany(page => page.Offers).Select(offer => offer.Id).Distinct().Count();

    /// <summary>Counted per distinct offer, because a repeated listing writes no new products.</summary>
    public int ProductCount => Pages
        .SelectMany(page => page.Offers)
        .DistinctBy(offer => offer.Id)
        .Sum(offer => offer.Products.Count);
}
