using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceClearOwnHistoryResult (September 2026) — whether the sold or expired offers were cleared.</summary>
[Message(ServerMessages.MarketplaceClearOwnHistoryResultComposer)]
public sealed partial record MarketplaceClearOwnHistoryResultComposer(bool Success);
