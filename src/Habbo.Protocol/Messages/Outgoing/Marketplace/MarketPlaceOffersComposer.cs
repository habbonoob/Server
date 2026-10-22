using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketPlaceOffersComposer</summary>
[Message(ServerMessages.MarketPlaceOffersComposer)]
public sealed partial record MarketPlaceOffersComposer(IReadOnlyList<MarketplaceOfferData> Offers, int TotalItemsFound);
