using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetNextTargetedOfferEvent</summary>
[Incoming(ClientMessages.GetNextTargetedOfferEvent)]
public sealed partial record GetNextTargetedOfferEvent;
