using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>GetMarketplaceOwnOffersMessageEvent — the player's own offers. September 2026 asks for one kind at a time:
/// open (1), sold (2) or expired (3).</summary>
[Incoming(ClientMessages.GetMarketplaceOwnOffersMessageEvent)]
public sealed partial record GetMarketplaceOwnOffersMessageEvent([property: WireSince(ClientReleases.September2026)] int? Category);
