namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>How many of an offer one purchase may take, and the bundle discount the client describes
/// (config "Catalog:BundleDiscount"). The client prices a bundle at the plain price times the amount,
/// so the discount it describes has to be none unless purchases hand out the free items too.</summary>
public sealed class BundleDiscountOptions
{
    public int MaxPurchaseSize { get; set; } = 100;

    public int BundleSize { get; set; }

    public int BundleDiscountSize { get; set; }

    public int BonusThreshold { get; set; }

    public List<int> AdditionalBonusDiscountThresholdQuantities { get; set; } = [];
}
