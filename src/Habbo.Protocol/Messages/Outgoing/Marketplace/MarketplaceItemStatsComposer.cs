using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceItemStatsComposer</summary>
[Message(ServerMessages.MarketplaceItemStatsComposer)]
public sealed partial record MarketplaceItemStatsComposer(
    int AveragePrice,
    int OfferCount,
    int HistoryLength,
    IReadOnlyList<MarketplaceDayStatData> Days,
    int FurniCategoryId,
    int FurniTypeId,
    [property: WireSince(ClientReleases.September2026)] int LowestCurrentPrice,
    [property: WireSince(ClientReleases.September2026)] int SuggestedPrice);
