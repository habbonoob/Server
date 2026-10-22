using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>PurchaseSnowWarGameTokensOfferEvent — buy one of the SnowStorm game token offers.</summary>
[Incoming(ClientMessages.PurchaseSnowWarGameTokensOfferEvent)]
public sealed partial record PurchaseSnowWarGameTokensOfferEvent(int OfferId);
