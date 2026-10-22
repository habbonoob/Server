namespace Habbo.Silo.Abstractions.Camera;

/// <summary>How buying a photo went: the photo furni made, or what was short.</summary>
public sealed record PhotoPurchase(bool Success, bool NoPhoto, bool NotEnoughCredits, bool NotEnoughDuckets, IReadOnlyList<int> ItemIds)
{
    public static PhotoPurchase Bought(IReadOnlyList<int> itemIds) => new(true, false, false, false, itemIds);

    public static readonly PhotoPurchase Nothing = new(false, true, false, false, []);

    public static readonly PhotoPurchase ShortOfCredits = new(false, false, true, false, []);

    public static readonly PhotoPurchase ShortOfDuckets = new(false, false, false, true, []);
}
