using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>BuyMarketplaceOfferMessageEvent</summary>
[Incoming(ClientMessages.BuyMarketplaceOfferMessageEvent)]
public sealed partial record BuyMarketplaceOfferMessageEvent(int OfferId);
