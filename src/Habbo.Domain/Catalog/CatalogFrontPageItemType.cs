namespace Habbo.Domain.Catalog;

/// <summary>What a front page promotion opens when clicked, as the client numbers it.</summary>
public enum CatalogFrontPageItemType
{
    /// <summary>A catalog page, by its name.</summary>
    Page = 0,

    /// <summary>An offer, by its id.</summary>
    Offer = 1,

    /// <summary>A product, by its code.</summary>
    Product = 2,
}
