using Habbo.Domain.Catalog;
using Habbo.Domain.Users;

namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>One product an offer hands over. A badge has no definition and carries its code
/// in ExtraParam; furniture names the definition and the class id the client renders.</summary>
[GenerateSerializer]
public sealed partial record ShopProduct(
    [property: Id(0)] int? DefinitionId,
    [property: Id(1)] int FurniClassId,
    [property: Id(2)] bool IsWallItem,
    [property: Id(3)] string ExtraParam,
    [property: Id(4)] int Amount,
    /// <summary>One of <see cref="ProductTypes"/>.</summary>
    [property: Id(5)] int Type = ProductTypes.Furniture,
    /// <summary>For an effect or a chat style: which one.</summary>
    [property: Id(6)] int? ClassId = null,
    [property: Id(7)] int DurationSeconds = 0)
{
    public bool IsBadge => Type == ProductTypes.Badge;
}

/// <summary>One purchasable offer, priced as a whole. It carries no page: the same offer is
/// listed on several pages, and the page is always known from the request that asked for it.</summary>
[GenerateSerializer]
public sealed partial record ShopOffer(
    [property: Id(0)] int OfferId,
    [property: Id(1)] string LocalizationId,
    [property: Id(2)] int PriceInCredits,
    [property: Id(3)] ClubLevel ClubLevel,
    [property: Id(4)] int MaxAmount,
    [property: Id(5)] List<ShopProduct> Products,
    /// <summary>The currency's own number, as the client reads it off the wire — 0 duckets,
    /// 5 diamonds. Kept as an int rather than the domain enum so this contracts assembly stays
    /// free of project references, which is what lets both it and Habbo.Domain be leaves.</summary>
    [property: Id(6)] int PriceInActivityPoints,
    [property: Id(7)] int ActivityPointType,
    [property: Id(8)] int PriceInSilver,
    [property: Id(9)] bool IsPet,
    [property: Id(10)] bool Giftable,
    [property: Id(11)] bool IsRentable,
    [property: Id(12)] string PreviewImage,
    /// <summary>A limited edition's run and how many of it are sold; 0 for an ordinary offer.</summary>
    [property: Id(13)] int LimitedStack = 0,
    [property: Id(14)] int LimitedSold = 0,
    /// <summary>When the offer goes on sale, if not yet.</summary>
    [property: Id(15)] DateTimeOffset? AvailableFrom = null,
    /// <summary>One of the club's monthly gifts: taken through the gift selection, never bought.</summary>
    [property: Id(16)] bool IsClubGift = false)
{
    public bool IsOnSale => AvailableFrom is null || AvailableFrom <= DateTimeOffset.UtcNow;

    public bool IsLimited => LimitedStack > 0;

    /// <summary>Silver is the collectibles' currency, which is not held yet.</summary>
    public bool NeedsSilver => PriceInSilver > 0;
}

/// <summary>One page of the catalog tree. Children are the pages nested under it.</summary>
public sealed record ShopPage(
    int PageId, string PageName, string Localization, int Icon, bool Visible, string LayoutCode,
    SecurityLevel MinRank, IReadOnlyList<string> Images, IReadOnlyList<string> Texts,
    IReadOnlyList<int> OfferIds, IReadOnlyList<ShopPage> Children);

/// <summary>Why a purchase was refused. The client only renders a generic alert, so this
/// exists for the server's own logging and tests rather than for the wire.</summary>
[GenerateSerializer]
public enum PurchaseFailure
{
    None,
    UnknownOffer,
    NotEnoughCredits,
    ClubRequired,
    InvalidAmount,

    /// <summary>Priced in a currency that is not held yet: silver.</summary>
    UnsupportedCurrency,

    /// <summary>The offer hands over nothing that can be granted.</summary>
    NothingToGrant,

    /// <summary>Not enough of the activity points the offer is priced in.</summary>
    NotEnoughActivityPoints,

    /// <summary>A limited edition whose run is sold out.</summary>
    SoldOut,

    /// <summary>Nobody by the name a gift was for.</summary>
    ReceiverNotFound,

    /// <summary>Not something that may be bought this way: an offer that is not giftable, a wrap that is not offered.</summary>
    NotAllowed,
}

/// <summary>What a completed purchase produced.</summary>
[GenerateSerializer]
public sealed partial record PurchaseResult(
    [property: Id(0)] PurchaseFailure Failure,
    [property: Id(1)] ShopOffer? Offer,
    [property: Id(2)] int RemainingCredits,
    [property: Id(3)] int ItemsGranted,
    /// <summary>For a shortfall in activity points: which kind.</summary>
    [property: Id(4)] int ActivityPointType = 0,
    /// <summary>The furniture handed over, by item id.</summary>
    [property: Id(5)] List<int>? ItemIds = null)
{
    public bool Success => Failure == PurchaseFailure.None;

    public static PurchaseResult Failed(PurchaseFailure failure) => new(failure, null, 0, 0);
}

/// <summary>The shop, loaded from the database. Changing what is for sale is a data change.</summary>
public interface IShopCatalog
{
    /// <summary>The page tree, already filtered to what this rank may see: the shop's, or the Builders Club
    /// warehouse's. The root page is the tree's anchor and is never rendered itself.</summary>
    Task<ShopPage?> GetTreeAsync(SecurityLevel rank, CancellationToken ct = default, bool buildersClub = false);

    /// <summary>One page's offers, or null when the page does not exist, is above this rank or belongs to the other
    /// catalog (the shop or the Builders Club warehouse).</summary>
    Task<(ShopPage Page, IReadOnlyList<ShopOffer> Offers)?> GetPageAsync(
        int pageId, SecurityLevel rank, CancellationToken ct = default, bool buildersClub = false);

    /// <summary>A Builders Club offer as listed on a warehouse page this rank may see, for borrowing; null for one that
    /// is not listed there.</summary>
    Task<ShopOffer?> GetBuildersClubOfferAsync(int pageId, int offerId, SecurityLevel rank, CancellationToken ct = default);

    Task<ShopOffer?> GetOfferAsync(int offerId, CancellationToken ct = default);

    /// <summary>An offer this rank may buy: one listed on a shop page it can see that has not expired, or a club
    /// membership (offered to everyone in the club window). Null for one that is hidden from it or listed nowhere, or
    /// only in the Builders Club warehouse, whose offers are borrowed rather than bought.</summary>
    Task<ShopOffer?> GetPurchasableOfferAsync(int offerId, SecurityLevel rank, CancellationToken ct = default);

    /// <summary>The front page's promotions that have not expired, by slot.</summary>
    Task<IReadOnlyList<ShopFrontPageItem>> GetFrontPageItemsAsync(CancellationToken ct = default);
}
