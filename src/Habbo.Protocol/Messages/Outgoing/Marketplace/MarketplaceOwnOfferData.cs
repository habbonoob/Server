using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceOwnOfferData — one of the player's own offers. September 2026 knows usable items (see
/// <see cref="MarketplaceOfferData"/>) and is told when a sold or expired offer sold or expired; November 2026 knows
/// NFTs, laid out as there.</summary>
[WireStruct]
public sealed partial record MarketplaceOwnOfferData(
    int OfferId,
    int Status,
    [property: WireUntil(ClientReleases.September2026)] int LegacyFurniType,
    [property: WireSince(ClientReleases.September2026)] int FurniType,
    [property: WireSince(ClientReleases.November2026), WireWhen("IsNft")] string? ProductCode,
    int FurniId,
    [property: WireSince(ClientReleases.November2026), WireWhen("IsNft")] int? NftFurniType,
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
    public bool IsNft => FurniType == MarketplaceOfferData.NftItem;
    public bool HasStatusTime => Status is MarketplaceOfferData.Sold or MarketplaceOfferData.Expired;

    /// <summary>The offers a client before November 2026 may be sent: all but the NFTs, a kind it cannot read.</summary>
    public static IReadOnlyList<MarketplaceOwnOfferData> WithoutNfts(IReadOnlyList<MarketplaceOwnOfferData> offers) =>
        offers.Any(o => o.IsNft) ? offers.Where(o => !o.IsNft).ToList() : offers;
}
