using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>TargetedOfferNotFoundComposer — no offer is made to this player now</summary>
[Message(ServerMessages.TargetedOfferNotFoundComposer)]
public sealed partial record TargetedOfferNotFoundComposer;
