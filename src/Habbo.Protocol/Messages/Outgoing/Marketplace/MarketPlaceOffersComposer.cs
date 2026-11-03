using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketPlaceOffersComposer. A client before November 2026 gets the list without its NFTs, which it could
/// not read: the server never trusts a search to have left them out.</summary>
[Message(ServerMessages.MarketPlaceOffersComposer)]
public sealed partial record MarketPlaceOffersComposer(
    [property: WireUntil(ClientReleases.November2026)] IReadOnlyList<MarketplaceOfferData> LegacyOffers,
    [property: WireSince(ClientReleases.November2026)] IReadOnlyList<MarketplaceOfferData> Offers,
    [property: WireUntil(ClientReleases.November2026)] int LegacyTotalItemsFound,
    [property: WireSince(ClientReleases.November2026)] int TotalItemsFound)
{
    public MarketPlaceOffersComposer(IReadOnlyList<MarketplaceOfferData> offers, int totalItemsFound)
        : this(MarketplaceOfferData.WithoutNfts(offers), offers, totalItemsFound, totalItemsFound)
    {
        LegacyTotalItemsFound = totalItemsFound - (offers.Count - LegacyOffers.Count);
    }
}
