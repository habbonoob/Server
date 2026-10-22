using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>PurchaseTargetedOfferEvent</summary>
[Incoming(ClientMessages.PurchaseTargetedOfferEvent)]
public sealed partial record PurchaseTargetedOfferEvent(int OfferId, int Quantity);
