using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>BuyNftMarketplaceOfferMessageEvent (November 2026) — buy the cheapest NFT on sale of a product (its
/// productdata code), for emeralds, at no more than the price the buyer was shown.</summary>
[Incoming(ClientMessages.BuyNftMarketplaceOfferMessageEvent)]
public sealed partial record BuyNftMarketplaceOfferMessageEvent(string ProductCode, int Price);
