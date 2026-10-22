using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetProductOfferEvent — one offer, for a link or an ad that names it</summary>
[Incoming(ClientMessages.GetProductOfferEvent)]
public sealed partial record GetProductOfferEvent(int OfferId);
