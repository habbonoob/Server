using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>Turns a shop offer into the client's offer shape. Shared by the page and the purchase
/// confirmation, which must describe the same offer identically.</summary>
internal static class CatalogOfferFactory
{
    public static CatalogOfferData From(ShopOffer offer) => CatalogOfferDataFactory.From(offer);
}
