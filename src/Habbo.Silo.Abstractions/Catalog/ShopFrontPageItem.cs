using Habbo.Domain.Catalog;

namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>One promotion on the catalog's front page, and what clicking it opens: a page name, an offer id or a
/// product code, as <see cref="Type"/> says.</summary>
[GenerateSerializer]
public sealed record ShopFrontPageItem(
    [property: Id(0)] int Position,
    [property: Id(1)] string Name,
    [property: Id(2)] string PromoImage,
    [property: Id(3)] CatalogFrontPageItemType Type,
    [property: Id(4)] string PageName,
    [property: Id(5)] int OfferId,
    [property: Id(6)] string ProductCode,
    [property: Id(7)] DateTimeOffset? ExpiresAt);
