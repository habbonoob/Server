using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>CancelAllMarketplaceOffers (September 2026) — take every open offer back.</summary>
[Incoming(ClientMessages.CancelAllMarketplaceOffersMessageEvent)]
public sealed partial record CancelAllMarketplaceOffersMessageEvent;
