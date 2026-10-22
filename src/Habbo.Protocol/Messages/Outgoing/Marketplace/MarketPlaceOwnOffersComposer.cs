using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketPlaceOwnOffersComposer</summary>
[Message(ServerMessages.MarketPlaceOwnOffersComposer)]
public sealed partial record MarketPlaceOwnOffersComposer(int CreditsWaiting, IReadOnlyList<MarketplaceOwnOfferData> Offers);
