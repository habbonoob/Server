using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceOfferData — an item type on sale: its cheapest offer, how many there are, and what it sold for
/// lately. September 2026 has a fourth kind, a usable item (a floor item that is used up, such as a crackable that takes
/// more than one hit), and is told whether it has been used; an older client takes it for a floor item.</summary>
[WireStruct]
public sealed partial record MarketplaceOfferData(
    int OfferId,
    int Status,
    [property: WireUntil(ClientReleases.September2026)] int LegacyFurniType,
    [property: WireSince(ClientReleases.September2026)] int FurniType,
    int FurniId,
    [property: WireWhen("HasStuffData")] FurniStuffData? StuffData,
    [property: WireWhen("IsWallItem")] string? ExtraData,
    [property: WireWhen("IsLimited")] int? UniqueSerialNumber,
    [property: WireWhen("IsLimited")] int? UniqueSeriesSize,
    [property: WireSince(ClientReleases.September2026), WireWhen("IsUsable")] bool? Used,
    int Price,
    int TimeLeftMinutes,
    int AveragePrice,
    int OfferCount)
{
    // the item's kind, as the marketplace numbers it; a limited edition is a floor item with its number
    public const int FloorItem = 1;
    public const int WallItem = 2;
    public const int LimitedItem = 3;
    public const int UsableItem = 4;

    // an offer's state
    public const int Open = 1;
    public const int Sold = 2;
    public const int Expired = 3;

    public bool HasStuffData => FurniType is FloorItem or UsableItem;
    public bool IsWallItem => FurniType == WallItem;
    public bool IsLimited => FurniType == LimitedItem;
    public bool IsUsable => FurniType == UsableItem;

    /// <summary>The kind a client before September 2026 knows: a usable item is a floor item to it.</summary>
    public static int Legacy(int furniType) => furniType == UsableItem ? FloorItem : furniType;
}
