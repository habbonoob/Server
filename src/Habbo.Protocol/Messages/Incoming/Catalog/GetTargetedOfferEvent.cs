using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetTargetedOfferEvent</summary>
[Incoming(ClientMessages.GetTargetedOfferEvent)]
public sealed partial record GetTargetedOfferEvent(int OfferId);
