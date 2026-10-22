using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>ClearMarketplaceOwnHistory (September 2026) — [category]: mark the sold (2) or expired (3) offers seen.</summary>
[Incoming(ClientMessages.ClearMarketplaceOwnHistoryMessageEvent)]
public sealed partial record ClearMarketplaceOwnHistoryMessageEvent(int Category);
