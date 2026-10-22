using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>CancelMarketplaceOfferMessageEvent</summary>
[Incoming(ClientMessages.CancelMarketplaceOfferMessageEvent)]
public sealed partial record CancelMarketplaceOfferMessageEvent(int OfferId);
