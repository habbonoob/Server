using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>One product inside an offer. The layout branches on the product type: a badge
/// ("b") carries only its code, and the client assumes a count of 1 rather than reading one.
/// Everything else carries the furni class id, count, and the limited-edition trailer.</summary>
[WireStruct]
public sealed partial record CatalogProductData(
    string ProductType,
    [property: WireWhen("IsFurniture")] int? FurniClassId,
    string ExtraParam,
    [property: WireWhen("IsFurniture")] int? ProductCount,
    [property: WireWhen("IsFurniture")] bool UniqueLimitedItem,
    [property: WireWhen("UniqueLimitedItem")] int? UniqueLimitedItemSeriesSize,
    [property: WireWhen("UniqueLimitedItem")] int? UniqueLimitedItemsLeft)
{
    /// <summary>The client's badge discriminator; anything else is furniture.</summary>
    public const string BadgeType = "b";

    public const string FloorType = "s";
    public const string WallType = "i";
    public const string EffectType = "e";
    public const string ChatStyleType = "chat_style";

    /// <summary>A rentable bot: the client draws it from the figure in the extra parameter.</summary>
    public const string BotType = "r";

    /// <summary>A pet: the page draws it from the kind and the palette chosen there.</summary>
    public const string PetType = "p";

    /// <summary>The branch the client takes: a badge stops after its code.</summary>
    public bool IsFurniture => ProductType != BadgeType;

    /// <summary>A plain, unlimited furniture product — what almost every offer holds.
    /// extraParam carries the colour for a multi-colour item: the client appends it to the
    /// asset name, and an item whose colours live under "class*n" has no un-suffixed icon to
    /// fall back on, so leaving it empty makes the catalog tile fail to load.</summary>
    public static CatalogProductData Furniture(
        string productType, int classId, int count, string extraParam = "") =>
        new(productType, classId, extraParam, count, UniqueLimitedItem: false, null, null);

    /// <summary>A limited edition: its run's size and how many of it are left.</summary>
    public static CatalogProductData Limited(
        string productType, int classId, int count, string extraParam, int seriesSize, int left) =>
        new(productType, classId, extraParam, count, UniqueLimitedItem: true, seriesSize, left);

    public static CatalogProductData Badge(string badgeCode) =>
        new(BadgeType, null, badgeCode, null, UniqueLimitedItem: false, null, null);
}
