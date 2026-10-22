using Habbo.Domain.Furniture;

namespace Habbo.Domain.Catalog;

/// <summary>One thing an offer hands over. A furniture product points at an item definition; a
/// badge carries its code in <see cref="ExtraParam"/>; an effect or a chat style names its id in
/// <see cref="ClassId"/>.</summary>
public sealed class CatalogProduct
{
    public int Id { get; set; }

    public int OfferId { get; set; }
    public CatalogOffer? Offer { get; set; }

    public CatalogProductType Type { get; set; }

    /// <summary>The furniture's definition; null for anything that is not furniture.</summary>
    public int? DefinitionId { get; set; }
    public ItemDefinition? Definition { get; set; }

    /// <summary>The badge code, or whatever else the product's UI collected.</summary>
    public string ExtraParam { get; set; } = string.Empty;

    /// <summary>How many of this product one purchase grants.</summary>
    public int Amount { get; set; } = 1;

    /// <summary>For an effect or a chat style: which one.</summary>
    public int? ClassId { get; set; }

    /// <summary>For an effect: how long it lasts once worn, in seconds; 0 for one that never wears off.</summary>
    public int DurationSeconds { get; set; }

    /// <summary>The client reads a shorter layout for a badge.</summary>
    public bool IsBadge => Type == CatalogProductType.Badge;
}
