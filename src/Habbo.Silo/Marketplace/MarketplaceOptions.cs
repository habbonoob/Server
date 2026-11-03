namespace Habbo.Silo.Marketplace;

/// <summary>The marketplace's rules (config section "Marketplace"), which the client also shows.</summary>
public sealed class MarketplaceOptions
{
    public bool Enabled { get; set; }

    /// <summary>The share of an average price that is commission, in percent, as the client shows prices without it.</summary>
    public int Commission { get; set; }

    public int TokenBatchPrice { get; set; }

    /// <summary>The tokens one batch buys; none turns tokens off, as Habbo has: an offer then costs nothing to make.</summary>
    public int TokenBatchSize { get; set; }

    public bool TokensRequired => TokenBatchSize > 0;

    public int OfferMinPrice { get; set; }

    public int OfferMaxPrice { get; set; }

    public int ExpirationHours { get; set; }

    /// <summary>The days an item's average price is taken over.</summary>
    public int AveragePricePeriodDays { get; set; }

    /// <summary>The days of sales an item's price history shows, day by day.</summary>
    public int StatsHistoryDays { get; set; } = 30;

    public int SellingFeePercentage { get; set; }

    public int RevenueLimit { get; set; }

    public int HalfTaxLimit { get; set; }

    /// <summary>The most item types one search lists.</summary>
    public int ListLimit { get; set; } = 500;

    /// <summary>The NFT marketplace (section "Marketplace:Nft"), which November 2026 clients have.</summary>
    public NftMarketplaceOptions Nft { get; set; } = new();
}

/// <summary>The NFT marketplace's rules, which November 2026 clients also show: NFTs (furni whose class name starts
/// "nft_") are sold for emeralds, one item an offer, the seller paid as each sells. The defaults are habbo.com's own, as
/// its MarketplaceConfiguration sends them (habbo.com, 2026-11-03: true, 2, 99999, 10080, 250, 2, 400000).</summary>
public sealed class NftMarketplaceOptions
{
    /// <summary>Off, the client is told so; NFT offers, buys and stats are refused, and searches list no NFT.</summary>
    public bool Enabled { get; set; }

    /// <summary>The least an NFT may be offered for, in emeralds: 2, as the credit marketplace, leaves the seller one
    /// after the fee.</summary>
    public int MinPrice { get; set; } = 2;

    /// <summary>The most an NFT may be offered for, in emeralds.</summary>
    public int MaxPrice { get; set; } = 99999;

    /// <summary>How long an NFT offer runs before it expires, in minutes.</summary>
    public int OfferDurationMinutes { get; set; } = 10080;

    /// <summary>The most NFT offers one player may have open at once; none means no limit.</summary>
    public int MaxOpenOffers { get; set; } = 250;

    /// <summary>The selling fee's flat part, in percent, as the credit marketplace's.</summary>
    public int SellingFeePercentage { get; set; } = 2;

    /// <summary>The price at which the fee's growing part reaches half, as the credit marketplace's.</summary>
    public int HalfTaxLimit { get; set; } = 400000;
}
