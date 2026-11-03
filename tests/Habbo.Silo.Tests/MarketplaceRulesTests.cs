using System.Buffers;
using Habbo.Domain.Furniture;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Outgoing.Marketplace;
using Habbo.Protocol.Wire;
using Habbo.Silo.Marketplace;

namespace Habbo.Silo.Tests;

/// <summary>The marketplace's fee and which offers a search lists, credit and NFT alike.</summary>
public class MarketplaceRulesTests
{
    [Theory]
    // price, fee %, half-tax limit, what the seller gets (the client's own sum: ceil(round(1000 * fee) / 1000))
    [InlineData(2, 2, 400000, 1)]
    [InlineData(100, 2, 400000, 97)]
    [InlineData(10000, 2, 400000, 9675)]
    [InlineData(99999, 2, 400000, 85499)]
    [InlineData(100, 0, 0, 100)]
    public void TheSellerGetsThePriceLessAFeeThatGrowsWithIt(int price, int feePercentage, int halfTaxLimit, int revenue) =>
        Assert.Equal(revenue, MarketplaceRules.RevenueOf(price, feePercentage, halfTaxLimit));

    [Theory]
    [InlineData(null, false, true, true)]
    [InlineData(null, true, true, false)]
    [InlineData(MarketplaceRules.AllOffers, false, true, true)]
    [InlineData(MarketplaceRules.AllOffers, true, true, true)]
    [InlineData(MarketplaceRules.CollectibleOffers, false, true, false)]
    [InlineData(MarketplaceRules.CollectibleOffers, true, true, true)]
    [InlineData(MarketplaceRules.FurniOffers, false, true, true)]
    [InlineData(MarketplaceRules.FurniOffers, true, true, false)]
    [InlineData(MarketplaceRules.AllOffers, true, false, false)]
    [InlineData(MarketplaceRules.CollectibleOffers, true, false, false)]
    public void ASearchListsNftsOnlyToNovember2026AskingForThemWithTheNftMarketplaceOn(int? source, bool isNft, bool nftEnabled, bool listed) =>
        Assert.Equal(listed, MarketplaceRules.Lists(source, isNft, nftEnabled));

    [Theory]
    [InlineData("nft_h23_dragon", true)]
    [InlineData("nft_", true)]
    [InlineData("NFT_h23_dragon", false)]
    [InlineData("throne_nft_", false)]
    [InlineData("throne", false)]
    public void AnNftIsFurniWhoseClassNameStartsNft(string className, bool isNft) =>
        Assert.Equal(isNft, ItemDefinition.IsNftClass(className));

    // What habbo.com was seen doing, recorded on 2026-11-03

    [Fact]
    public void NftRowsAreNumberedDownFromMinusOneOverTheProductsFound()
    {
        var ids = MarketplaceRules.NftRowIds([("nft_b", 30), ("nft_a", 10), ("nft_c", 20)]);
        Assert.Equal(-1, ids["nft_a"]);
        Assert.Equal(-2, ids["nft_c"]);
        Assert.Equal(-3, ids["nft_b"]);

        // one product found is -1, as habbo.com's search for "bunny"
        Assert.Equal(-1, MarketplaceRules.NftRowIds([("nft_sc25_bigbunnybed", 500)])["nft_sc25_bigbunnybed"]);
    }

    private static readonly DateTimeOffset Now = new(2026, 11, 3, 22, 16, 0, TimeSpan.Zero);

    [Fact]
    public void AnNftsHistoryRunsDayByDayFromItsFirstSaleToYesterday()
    {
        // nft_a0_clubsofa: four sold six days ago and one two days ago, at 100; today's sale is not a point
        var sales = Enumerable.Repeat((Now.AddDays(-6), 100), 4).Append((Now.AddDays(-2), 100)).Append((Now.AddHours(-1), 220));
        var history = MarketplaceRules.NftHistory(sales, Now, 30);

        Assert.Equal(
        [
            new MarketplaceDayStatData(-6, 100, 4), new MarketplaceDayStatData(-5, 0, 0), new MarketplaceDayStatData(-4, 0, 0),
            new MarketplaceDayStatData(-3, 0, 0), new MarketplaceDayStatData(-2, 100, 1), new MarketplaceDayStatData(-1, 0, 0),
        ], history);

        // and with it the stats are habbo.com's, byte for byte (habbo.com, 2026-11-03)
        var buffer = new ArrayBufferWriter<byte>();
        new NftMarketplaceItemStatsMessageComposer(100, 1, 30, history, 220, 220).Compose(new MessageWriter(buffer, ClientReleases.November2026));
        Assert.Equal(Convert.FromHexString("00000064000000010000001E00000006FFFFFFFA0000006400000004FFFFFFFB0000000000000000FFFFFFFC0000000000000000"
                                           + "FFFFFFFD0000000000000000FFFFFFFE0000006400000001FFFFFFFF0000000000000000000000DC000000DC"),
            buffer.WrittenSpan.ToArray());
    }

    [Fact]
    public void AnNftNeverSoldBeforeTodayHasNoHistory()
    {
        Assert.Empty(MarketplaceRules.NftHistory([(Now.AddHours(-2), 50)], Now, 30));
        Assert.Empty(MarketplaceRules.NftHistory([(Now.AddDays(-31), 50)], Now, 30));
    }

    private static NftProductMap Map() => NftProductMap.Build(
        [
            new NftDefinition(1, "nft_a0club_sofa", "Club sofa"),
            new NftDefinition(2, "nft_sc25_bigbunnybed", "Bunny Cuddle Bed"),
            new NftDefinition(3, "nft_table_plasto_4leg*2", "Occasional Table"),
            new NftDefinition(4, "nft_table_plasto_4leg*5", "Occasional Table"),
            new NftDefinition(5, "nft_h26_witchchair", "Witch chair"),
        ],
        [
            new NftProduct("nft_a0_clubsofa", "Club sofa"),
            new NftProduct("nft_sc25_bigbunnybed", "Bunny Cuddle Bed"),
            new NftProduct("nft_table_occasional", "Occasional Table"),
            new NftProduct("nft_h26_witchchair_offer", "Witch chair"),
        ],
        [new NftCatalogName(5, "nft_h26_witchchair_offer")]);

    private static ItemDefinition Definition(int id, string className) => new() { Id = id, ClassName = className };

    [Fact]
    public void AnNftIsListedUnderItsProductCode()
    {
        var map = Map();
        // its class name when that is a product, as 1301 of habbo.com's 1584 NFT furni
        Assert.Equal("nft_sc25_bigbunnybed", map.CodeOf(Definition(2, "nft_sc25_bigbunnybed")));
        // the one spare product its name describes, as nft_a0club_sofa is listed as nft_a0_clubsofa
        Assert.Equal("nft_a0_clubsofa", map.CodeOf(Definition(1, "nft_a0club_sofa")));
        // a catalog offer's name first
        Assert.Equal("nft_h26_witchchair_offer", map.CodeOf(Definition(5, "nft_h26_witchchair")));
        // two furni of one name share no product: each keeps its class name
        Assert.Equal("nft_table_plasto_4leg*2", map.CodeOf(Definition(3, "nft_table_plasto_4leg*2")));
        Assert.Equal("nft_table_plasto_4leg*5", map.CodeOf(Definition(4, "nft_table_plasto_4leg*5")));
    }

    [Fact]
    public void WhatHabboComListsAnNftAsComesFirst()
    {
        // nft_hc_crpt is listed as nft_a0_clubrug (habbo.com, 2026-11-03): no product data or catalog name says so
        var map = NftProductMap.Build([new NftDefinition(7, "nft_hc_crpt", "Persian Carpet")],
            [new NftProduct("nft_hc_crpt", "Persian Carpet")], []);
        Assert.Equal("nft_a0_clubrug", map.CodeOf(Definition(7, "nft_hc_crpt")));
        Assert.Equal([7], map.DefinitionsOf("nft_a0_clubrug"));
        Assert.Empty(map.DefinitionsOf("nft_hc_crpt"));
        Assert.Equal(61, NftProductCodes.Recorded.Count);
    }

    [Fact]
    public void AClassNameThatIsNotItsProductCodeNamesNoProduct()
    {
        var map = Map();
        Assert.Equal([1], map.DefinitionsOf("nft_a0_clubsofa"));
        Assert.Empty(map.DefinitionsOf("nft_a0club_sofa"));
        Assert.Empty(map.DefinitionsOf("nft_does_not_exist"));
    }
}
