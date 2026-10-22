using Habbo.Domain.Catalog;
using Habbo.Domain.Users;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>One purchasable offer on a catalog page. An offer holds one or more products —
/// a bundle is simply an offer with several — and the price is per offer, not per product.
/// IsPet is read by this client but exposed through no getter, so it looks dead here; it still
/// occupies a byte, and leaving it out shifts the preview image.</summary>
[WireStruct]
public sealed partial record CatalogOfferData(
    int OfferId,
    string LocalizationId,
    bool IsRent,
    int PriceInCredits,
    int PriceInActivityPoints,
    int ActivityPointType,
    int PriceInSilver,
    bool Giftable,
    IReadOnlyList<CatalogProductData> Products,
    int ClubLevel,
    bool BundlePurchaseAllowed,
    bool IsPet,
    string PreviewImage)
{
    /// <summary>An offer bought with credits alone.</summary>
    public static CatalogOfferData ForCredits(
        int offerId, string localizationId, int priceInCredits, IReadOnlyList<CatalogProductData> products) =>
        new(offerId, localizationId, IsRent: false, priceInCredits,
            PriceInActivityPoints: 0, ActivityPointType: 0, PriceInSilver: 0,
            Giftable: false, products, ClubLevel: 0, BundlePurchaseAllowed: false,
            IsPet: false, PreviewImage: "");
}
