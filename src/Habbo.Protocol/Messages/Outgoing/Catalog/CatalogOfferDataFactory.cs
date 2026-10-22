using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>A shop offer as the client's offer layout carries it, with every price and each product.</summary>
public static class CatalogOfferDataFactory
{
    public static CatalogOfferData From(ShopOffer offer) => new(
        offer.OfferId,
        offer.LocalizationId,
        offer.IsRentable,
        offer.PriceInCredits,
        offer.PriceInActivityPoints,
        offer.ActivityPointType,
        offer.PriceInSilver,
        offer.Giftable,
        offer.Products.Select(product => From(offer, product)).ToList(),
        (int)offer.ClubLevel,
        BundlePurchaseAllowed: offer.MaxAmount > 1,
        offer.IsPet,
        offer.PreviewImage);

    private static CatalogProductData From(ShopOffer offer, ShopProduct product) => product.Type switch
    {
        ProductTypes.Badge => CatalogProductData.Badge(product.ExtraParam),
        ProductTypes.Effect => CatalogProductData.Furniture(CatalogProductData.EffectType, product.ClassId ?? 0, product.Amount, product.ExtraParam),
        ProductTypes.Pet => CatalogProductData.Furniture(CatalogProductData.PetType, product.ClassId ?? 0, product.Amount, product.ExtraParam),
        ProductTypes.Bot => CatalogProductData.Furniture(CatalogProductData.BotType, product.ClassId ?? 0, product.Amount, product.ExtraParam),
        ProductTypes.ChatStyle => CatalogProductData.Furniture(CatalogProductData.ChatStyleType, product.ClassId ?? 0, product.Amount, product.ExtraParam),
        _ when offer.IsLimited => CatalogProductData.Limited(
            product.IsWallItem ? CatalogProductData.WallType : CatalogProductData.FloorType,
            product.FurniClassId, product.Amount, product.ExtraParam, offer.LimitedStack, offer.LimitedStack - offer.LimitedSold),
        _ => CatalogProductData.Furniture(
            product.IsWallItem ? CatalogProductData.WallType : CatalogProductData.FloorType,
            product.FurniClassId,
            product.Amount,
            product.ExtraParam),
    };
}
