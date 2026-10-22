using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>BuyMarketplaceTokensMessageEvent</summary>
[Incoming(ClientMessages.BuyMarketplaceTokensMessageEvent)]
public sealed partial record BuyMarketplaceTokensMessageEvent;
