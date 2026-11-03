using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>NftMarketplaceItemStatsMessageComposer (November 2026) — an NFT product's sales: as
/// <see cref="MarketplaceItemStatsComposer"/> without the furni category and type, the product being what was asked
/// about.</summary>
[Message(ServerMessages.NftMarketplaceItemStatsMessageComposer)]
public sealed partial record NftMarketplaceItemStatsMessageComposer(
    int AveragePrice,
    int OfferCount,
    int HistoryLength,
    IReadOnlyList<MarketplaceDayStatData> Days,
    int LowestCurrentPrice,
    int SuggestedPrice);
