using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>SetTargetedOfferStateEvent</summary>
[Incoming(ClientMessages.SetTargetedOfferStateEvent)]
public sealed partial record SetTargetedOfferStateEvent(int OfferId, int State);
