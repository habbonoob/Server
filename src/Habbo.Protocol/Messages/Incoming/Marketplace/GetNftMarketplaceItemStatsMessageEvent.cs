using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>GetNftMarketplaceItemStatsMessageEvent (November 2026) — an NFT product's sales, by its product code (its
/// productdata code; the inventory asks with the furni's class name, which is not always the same).</summary>
[Incoming(ClientMessages.GetNftMarketplaceItemStatsMessageEvent)]
public sealed partial record GetNftMarketplaceItemStatsMessageEvent(string ProductCode);
