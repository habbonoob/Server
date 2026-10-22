using Habbo.Domain.Catalog;
using Habbo.Domain.Users;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>The offer echoed back in a purchase confirmation. Deliberately not
/// CatalogOfferData: the client's PurchaseOKMessageOfferData drops the silver price and the
/// preview image, so reusing the page struct here would desync the packet by two fields.</summary>
[WireStruct]
public sealed partial record PurchaseOfferData(
    int OfferId,
    string LocalizationId,
    bool IsRent,
    int PriceInCredits,
    int PriceInActivityPoints,
    int ActivityPointType,
    bool Giftable,
    IReadOnlyList<CatalogProductData> Products,
    int ClubLevel,
    bool BundlePurchaseAllowed)
{
    /// <summary>Echoes back the offer that was bought, which is all the client needs.</summary>
    public static PurchaseOfferData From(CatalogOfferData offer) =>
        new(offer.OfferId, offer.LocalizationId, offer.IsRent, offer.PriceInCredits,
            offer.PriceInActivityPoints, offer.ActivityPointType, offer.Giftable,
            offer.Products, offer.ClubLevel, offer.BundlePurchaseAllowed);
}
