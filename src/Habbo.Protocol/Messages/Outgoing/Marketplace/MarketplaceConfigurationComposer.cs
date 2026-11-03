using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceConfigurationComposer. November 2026 reads the NFT marketplace's rules after the rest, when
/// there are bytes left: whether it is on, its price range, how many minutes an offer runs, how many may be open at
/// once, and its selling fee (percentage and half-tax limit, worked out as the credit marketplace's).</summary>
[Message(ServerMessages.MarketplaceConfigurationComposer)]
public sealed partial record MarketplaceConfigurationComposer(
    bool IsEnabled,
    int Commission,
    int TokenBatchPrice,
    int TokenBatchSize,
    int OfferMinPrice,
    int OfferMaxPrice,
    int ExpirationHours,
    int AveragePricePeriod,
    int SellingFeePercentage,
    int RevenueLimit,
    int HalfTaxLimit,
    [property: WireSince(ClientReleases.November2026)] bool NftMarketplaceEnabled = false,
    [property: WireSince(ClientReleases.November2026)] int NftOfferMinPrice = 0,
    [property: WireSince(ClientReleases.November2026)] int NftOfferMaxPrice = 0,
    [property: WireSince(ClientReleases.November2026)] int NftOfferDurationMinutes = 0,
    [property: WireSince(ClientReleases.November2026)] int NftMaximumOpenOffers = 0,
    [property: WireSince(ClientReleases.November2026)] int NftSellingFeePercentage = 0,
    [property: WireSince(ClientReleases.November2026)] int NftHalfTaxLimit = 0);
