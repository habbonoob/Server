using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceOwnOfferData — one of the player's own offers. September 2026 knows usable items (see
/// <see cref="MarketplaceOfferData"/>) and is told when a sold or expired offer sold or expired.</summary>
[WireStruct]
public sealed partial record MarketplaceOwnOfferData(
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
    /// <summary>When it sold or expired, in milliseconds since 1970.</summary>
    [property: WireSince(ClientReleases.September2026), WireWhen("HasStatusTime")] long? StatusTime)
{
    public bool HasStuffData => FurniType is MarketplaceOfferData.FloorItem or MarketplaceOfferData.UsableItem;
    public bool IsWallItem => FurniType == MarketplaceOfferData.WallItem;
    public bool IsLimited => FurniType == MarketplaceOfferData.LimitedItem;
    public bool IsUsable => FurniType == MarketplaceOfferData.UsableItem;
    public bool HasStatusTime => Status is MarketplaceOfferData.Sold or MarketplaceOfferData.Expired;
}
