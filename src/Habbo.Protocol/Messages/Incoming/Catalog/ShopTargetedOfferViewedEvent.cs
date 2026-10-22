using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>ShopTargetedOfferViewedEvent</summary>
[Incoming(ClientMessages.ShopTargetedOfferViewedEvent)]
public sealed partial record ShopTargetedOfferViewedEvent(int OfferId, int TrackingState);
