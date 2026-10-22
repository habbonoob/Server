using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>GetMarketplaceCanMakeOfferMessageEvent</summary>
[Incoming(ClientMessages.GetMarketplaceCanMakeOfferMessageEvent)]
public sealed partial record GetMarketplaceCanMakeOfferMessageEvent;
