using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>SnowWarGameTokensMessageComposer — the offers of SnowStorm game tokens.</summary>
[Message(ServerMessages.SnowWarGameTokensMessageComposer)]
public sealed partial record SnowWarGameTokensMessageComposer(IReadOnlyList<SnowWarGameTokenOfferData> Offers);
