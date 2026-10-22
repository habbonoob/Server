using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>TargetedOfferComposer — an offer made to this player</summary>
[Message(ServerMessages.TargetedOfferComposer)]
public sealed partial record TargetedOfferComposer(TargetedOfferData Offer);
