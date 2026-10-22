using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>GetMarketplaceItemStatsEvent — an item type's sales. September 2026 names the poster for a poster, whose
/// every design is its own item to the marketplace.</summary>
[Incoming(ClientMessages.GetMarketplaceItemStatsEvent)]
public sealed partial record GetMarketplaceItemStatsEvent(int FurniType, int FurniId, [property: WireSince(ClientReleases.September2026)] string? PosterId);
