using Habbo.Protocol.Messages.Outgoing.Marketplace;

namespace Habbo.Silo.Marketplace;

/// <summary>The marketplace's arithmetic and listing rules, apart from the database so they can be held to.</summary>
public static class MarketplaceRules
{
    // which offers a November 2026 search asks for, as its menu lists them: "All", "Collectibles", "Furni"
    public const int AllOffers = 0;
    public const int CollectibleOffers = 1;
    public const int FurniOffers = 2;

    /// <summary>What the seller receives of a price: the price less the selling fee, which grows with the price, worked
    /// out as the client works it out (the same for credits and, with the NFT marketplace's rules, emeralds).</summary>
    public static int RevenueOf(int price, int sellingFeePercentage, int halfTaxLimit)
    {
        var growing = halfTaxLimit > 0 ? 0.5 * price / halfTaxLimit : 0;
        var fee = Math.Ceiling(Math.Round(1000 * (price * (sellingFeePercentage / 100.0 + growing))) / 1000);
        return price - (int)fee;
    }

    /// <summary>Whether a search lists an offer: an NFT only with the NFT marketplace on and to a November 2026 client
    /// asking for everything or for collectibles; furni to anyone not asking for collectibles alone, as habbo.com: 1 lists
    /// NFT rows only (299 found), 2 none (10903), 0 both together (11202) (habbo.com, 2026-11-03). A
    /// client before November 2026 says nothing of which (null) and never sees an NFT, since it cannot read one.</summary>
    public static bool Lists(int? offerSource, bool isNft, bool nftEnabled) =>
        isNft
            ? nftEnabled && offerSource is AllOffers or CollectibleOffers
            : offerSource != CollectibleOffers;

    /// <summary>The offer id an NFT search row carries: not an offer's (a buy names the product), but minus the product's
    /// place among the NFT products the search found, counted from 1. habbo.com numbers them so (habbo.com, 2026-11-03: one
    /// product found is -1, fifteen are -1 to -15, the whole market runs to about -298, and a product keeps its number
    /// across searches finding the same products). The order it counts them in is not known; ours is each product's oldest
    /// offer, which keeps the numbers steady while the market does.</summary>
    public static Dictionary<string, int> NftRowIds(IEnumerable<(string ProductCode, int OldestOfferId)> products) =>
        products.OrderBy(p => p.OldestOfferId).ThenBy(p => p.ProductCode, StringComparer.Ordinal)
            .Select((p, index) => (p.ProductCode, Id: -(index + 1)))
            .ToDictionary(p => p.ProductCode, p => p.Id, StringComparer.Ordinal);

    /// <summary>An NFT product's price history as habbo.com gives it: a point for every day from the earliest it sold on
    /// (within the history's days) to yesterday, a day without sales at 0 and 0, and none for today (habbo.com, 2026-11-03,
    /// nft_a0_clubsofa's -6 to -1, its four days without sales included, and no day 0 for either product recorded).</summary>
    public static List<MarketplaceDayStatData> NftHistory(IEnumerable<(DateTimeOffset SoldAt, int Price)> sales, DateTimeOffset now,
        int historyDays)
    {
        var byDay = sales.Select(s => (Offset: (int)(now.UtcDateTime.Date - s.SoldAt.UtcDateTime.Date).TotalDays, s.Price))
            .Where(s => s.Offset >= 1 && s.Offset <= historyDays)
            .GroupBy(s => s.Offset)
            .ToDictionary(g => g.Key, g => (Average: (int)Math.Round(g.Average(s => s.Price)), Count: g.Count()));
        if (byDay.Count == 0)
            return [];

        return Enumerable.Range(1, byDay.Keys.Max()).Reverse()
            .Select(offset => byDay.TryGetValue(offset, out var day)
                ? new MarketplaceDayStatData(-offset, day.Average, day.Count)
                : new MarketplaceDayStatData(-offset, 0, 0))
            .ToList();
    }
}
