using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketPlaceOwnOffersComposer. A client before November 2026 gets the list without its NFTs, which it could
/// not read. What is waiting is credits only: an NFT's emeralds are paid as it sells.</summary>
[Message(ServerMessages.MarketPlaceOwnOffersComposer)]
public sealed partial record MarketPlaceOwnOffersComposer(
    int CreditsWaiting,
    [property: WireUntil(ClientReleases.November2026)] IReadOnlyList<MarketplaceOwnOfferData> LegacyOffers,
    [property: WireSince(ClientReleases.November2026)] IReadOnlyList<MarketplaceOwnOfferData> Offers)
{
    public MarketPlaceOwnOffersComposer(int creditsWaiting, IReadOnlyList<MarketplaceOwnOfferData> offers)
        : this(creditsWaiting, MarketplaceOwnOfferData.WithoutNfts(offers), offers)
    {
    }
}
