using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetSnowWarGameTokensOfferEvent — the offers of SnowStorm game tokens.</summary>
[Incoming(ClientMessages.GetSnowWarGameTokensOfferEvent)]
public sealed partial record GetSnowWarGameTokensOfferEvent;
