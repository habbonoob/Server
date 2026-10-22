using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceCancelAllOffersResult (September 2026) — the open offers taken off the marketplace.</summary>
[Message(ServerMessages.MarketplaceCancelAllOffersResultComposer)]
public sealed partial record MarketplaceCancelAllOffersResultComposer(IReadOnlyList<int> OfferIds, bool Success);
