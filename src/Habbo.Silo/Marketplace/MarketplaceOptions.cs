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
}
