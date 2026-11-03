using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceOfferData — an item type on sale: its cheapest offer, how many there are, and what it sold for
/// lately. September 2026 has a fourth kind, a usable item (a floor item that is used up, such as a crackable that takes
/// more than one hit), and is told whether it has been used; an older client takes it for a floor item. November 2026
/// has a fifth, an NFT on sale for emeralds, named by its product code before its furni id and followed by whether it is
/// a floor or a wall item; no older client knows it, so none is ever sent one (see <see cref="WithoutNfts"/>).</summary>
[WireStruct]
public sealed partial record MarketplaceOfferData(
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
    int OfferCount)
{
    // the item's kind, as the marketplace numbers it; a limited edition is a floor item with its number
    public const int FloorItem = 1;
    public const int WallItem = 2;
    public const int LimitedItem = 3;
    public const int UsableItem = 4;
    public const int NftItem = 5;

    // an offer's state
    public const int Open = 1;
    public const int Sold = 2;
    public const int Expired = 3;

    public bool HasStuffData => FurniType is FloorItem or UsableItem;
    public bool IsWallItem => FurniType == WallItem;
    public bool IsLimited => FurniType == LimitedItem;
    public bool IsUsable => FurniType == UsableItem;
    public bool IsNft => FurniType == NftItem;

    /// <summary>The kind a client before September 2026 knows: a usable item is a floor item to it.</summary>
    public static int Legacy(int furniType) => furniType == UsableItem ? FloorItem : furniType;

    /// <summary>The offers a client before November 2026 may be sent: all but the NFTs, a kind it cannot read.</summary>
    public static IReadOnlyList<MarketplaceOfferData> WithoutNfts(IReadOnlyList<MarketplaceOfferData> offers) =>
        offers.Any(o => o.IsNft) ? offers.Where(o => !o.IsNft).ToList() : offers;
}
