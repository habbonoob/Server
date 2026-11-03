using System.Buffers;
using Habbo.Protocol.Composers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Incoming.Marketplace;
using Habbo.Protocol.Messages.Outgoing.Collectibles;
using Habbo.Protocol.Messages.Outgoing.Marketplace;
using Habbo.Protocol.Releases;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Tests;

/// <summary>November 2026's NFT marketplace, in the order the client (WIN63-202611031222) writes and reads it.</summary>
public class NftMarketplaceWireTests
{
    private static byte[] Payload(IComposer composer, string release)
    {
        var buffer = new ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer, release));
        return buffer.WrittenSpan.ToArray();
    }

    private static MessageReader Read(IComposer composer, string release) => new(Payload(composer, release));

    private static MessageReader Incoming(Action<MessageWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        write(new MessageWriter(buffer));
        return new MessageReader(buffer.WrittenMemory) { Release = ClientReleases.November2026 };
    }

    private static MarketplaceConfigurationComposer Configuration => new(true, 1, 0, 0, 2, 99999, 168, 7, 2, 100000, 400000,
        NftMarketplaceEnabled: true, NftOfferMinPrice: 2, NftOfferMaxPrice: 5000, NftOfferDurationMinutes: 10080,
        NftMaximumOpenOffers: 50, NftSellingFeePercentage: 3, NftHalfTaxLimit: 200000);

    [Fact]
    public void TheConfigurationEndsWithTheNftMarketplacesRulesForNovember2026Only()
    {
        var march = Payload(Configuration, ClientReleases.March2026);
        Assert.Equal(1 + 10 * 4, march.Length);
        Assert.Equal(march, Payload(Configuration, ClientReleases.September2026));
        Assert.Equal(march, Payload(Configuration, ClientReleases.October2026));

        var november = Payload(Configuration, ClientReleases.November2026);
        Assert.Equal(march, november[..march.Length]);
        var rest = new MessageReader(november.AsMemory(march.Length));
        Assert.True(rest.ReadBoolean());
        Assert.Equal([2, 5000, 10080, 50, 3, 200000], Enumerable.Range(0, 6).Select(_ => rest.ReadInt()));
        Assert.Equal(0, rest.Remaining);
    }

    [Fact]
    public void AnNftMarketplaceTurnedOffIsSaidSo()
    {
        var off = new MarketplaceConfigurationComposer(true, 1, 0, 0, 2, 99999, 168, 7, 2, 100000, 400000);
        var november = Payload(off, ClientReleases.November2026);
        var rest = new MessageReader(november.AsMemory(1 + 10 * 4));
        Assert.False(rest.ReadBoolean());
        Assert.Equal(6 * 4, rest.Remaining);
    }

    private static MarketplaceOfferData NftOffer => new(-42, MarketplaceOfferData.Open, MarketplaceOfferData.Legacy(MarketplaceOfferData.NftItem),
        MarketplaceOfferData.NftItem, "nft_h23_dragon", 9001, MarketplaceOfferData.WallItem, null, null, null, null, null, 150, 600, 120, 3);

    private static MarketplaceOfferData WallOffer => new(7, MarketplaceOfferData.Open, MarketplaceOfferData.WallItem, MarketplaceOfferData.WallItem,
        null, 4001, null, null, "2005", null, null, null, 30, 60, 25, 1);

    [Fact]
    public void AnNftOfferIsItsProductCodeFurniIdAndNftFurniTypeForNovember2026()
    {
        var composer = new MarketPlaceOffersComposer([NftOffer], 1);

        var november = Read(composer, ClientReleases.November2026);
        Assert.Equal(1, november.ReadInt());
        Assert.Equal((-42, MarketplaceOfferData.Open, MarketplaceOfferData.NftItem), (november.ReadInt(), november.ReadInt(), november.ReadInt()));
        Assert.Equal("nft_h23_dragon", november.ReadString());
        Assert.Equal((9001, MarketplaceOfferData.WallItem), (november.ReadInt(), november.ReadInt()));
        Assert.Equal((150, 600, 120, 3), (november.ReadInt(), november.ReadInt(), november.ReadInt(), november.ReadInt()));
        Assert.Equal(1, november.ReadInt());
        Assert.Equal(0, november.Remaining);
    }

    [Fact]
    public void NoClientBeforeNovember2026IsSentAnNftOffer()
    {
        var composer = new MarketPlaceOffersComposer([NftOffer, WallOffer], 2);

        foreach (var release in new[] { ClientReleases.March2026, ClientReleases.September2026, ClientReleases.October2026 })
        {
            var older = Read(composer, release);
            Assert.Equal((1, 7), (older.ReadInt(), older.ReadInt()));
            Assert.Equal((MarketplaceOfferData.Open, MarketplaceOfferData.WallItem, 4001), (older.ReadInt(), older.ReadInt(), older.ReadInt()));
            Assert.Equal("2005", older.ReadString());
            Assert.Equal((30, 60, 25, 1), (older.ReadInt(), older.ReadInt(), older.ReadInt(), older.ReadInt()));
            Assert.Equal(1, older.ReadInt());
            Assert.Equal(0, older.Remaining);
        }

        var november = Read(composer, ClientReleases.November2026);
        Assert.Equal((2, -42), (november.ReadInt(), november.ReadInt()));
    }

    [Fact]
    public void AnOwnNftOfferIsLaidOutAsInTheSearchThenWhenItSold()
    {
        var sold = new MarketplaceOwnOfferData(42, MarketplaceOfferData.Sold, MarketplaceOfferData.NftItem, MarketplaceOfferData.NftItem,
            "nft_h23_dragon", 9001, MarketplaceOfferData.FloorItem, null, null, null, null, null, 150, 0, 120, StatusTime: 1_790_000_000_000);
        var composer = new MarketPlaceOwnOffersComposer(10, [sold]);

        var november = Read(composer, ClientReleases.November2026);
        Assert.Equal((10, 1), (november.ReadInt(), november.ReadInt()));
        Assert.Equal((42, MarketplaceOfferData.Sold, MarketplaceOfferData.NftItem), (november.ReadInt(), november.ReadInt(), november.ReadInt()));
        Assert.Equal("nft_h23_dragon", november.ReadString());
        Assert.Equal((9001, MarketplaceOfferData.FloorItem), (november.ReadInt(), november.ReadInt()));
        Assert.Equal((150, 0, 120), (november.ReadInt(), november.ReadInt(), november.ReadInt()));
        Assert.Equal(1_790_000_000_000, november.ReadLong());
        Assert.Equal(0, november.Remaining);

        var october = Read(composer, ClientReleases.October2026);
        Assert.Equal((10, 0), (october.ReadInt(), october.ReadInt()));
        Assert.Equal(0, october.Remaining);
    }

    [Fact]
    public void AnNftOfferIsItsKindItemAndPrice()
    {
        var r = Incoming(w => { w.WriteInt(2); w.WriteInt(1234); w.WriteInt(150); });
        Assert.Equal(new MakeNftMarketplaceOfferMessageEvent(2, 1234, 150), MakeNftMarketplaceOfferMessageEvent.Read(r));
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void BuyingAnNftIsItsProductCodeAndThePriceShown()
    {
        var r = Incoming(w => { w.WriteString("nft_h23_dragon"); w.WriteInt(150); });
        Assert.Equal(new BuyNftMarketplaceOfferMessageEvent("nft_h23_dragon", 150), BuyNftMarketplaceOfferMessageEvent.Read(r));
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void AnNftsStatsAreAskedForByProductCode()
    {
        var r = Incoming(w => w.WriteString("nft_h23_dragon"));
        Assert.Equal(new GetNftMarketplaceItemStatsMessageEvent("nft_h23_dragon"), GetNftMarketplaceItemStatsMessageEvent.Read(r));
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void ANovember2026SearchSaysWhichOffers()
    {
        var r = Incoming(w => { w.WriteInt(-1); w.WriteInt(-1); w.WriteString(""); w.WriteInt(2); w.WriteBoolean(true); w.WriteInt(1); });
        Assert.Equal(new GetMarketplaceOffersMessageEvent(-1, -1, "", 2, true, 1), GetMarketplaceOffersMessageEvent.Read(r));
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void TheNftOfferResultIsOneInt()
    {
        var r = Read(new NftMarketplaceMakeOfferResultMessageComposer(NftMarketplaceMakeOfferResultMessageComposer.Posted), ClientReleases.November2026);
        Assert.Equal(1, r.ReadInt());
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void TheNftBuyResultIsItsCodeTheOfferAsALongAndThePrice()
    {
        var r = Read(new NftMarketplaceBuyResultMessageComposer(NftMarketplaceBuyResultMessageComposer.Success, 42, 150), ClientReleases.November2026);
        Assert.Equal("SUCCESS", r.ReadString());
        Assert.Equal(42L, r.ReadLong());
        Assert.Equal(150, r.ReadInt());
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void AnNftsStatsAreTheItemStatsWithoutTheFurniTypeAndCategory()
    {
        var composer = new NftMarketplaceItemStatsMessageComposer(120, 3, 30, [new MarketplaceDayStatData(-1, 110, 2)], 100, 100);

        var r = Read(composer, ClientReleases.November2026);
        Assert.Equal((120, 3, 30, 1), (r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt()));
        Assert.Equal((-1, 110, 2), (r.ReadInt(), r.ReadInt(), r.ReadInt()));
        Assert.Equal((100, 100), (r.ReadInt(), r.ReadInt()));
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void TheEmeraldBalanceIsOneInt()
    {
        var r = Read(new EmeraldBalanceMessageComposer(77), ClientReleases.March2026);
        Assert.Equal(77, r.ReadInt());
        Assert.Equal(0, r.Remaining);
    }

    [Theory]
    [InlineData("MakeNftMarketplaceOfferMessageComposer", 3923, ClientMessages.MakeNftMarketplaceOfferMessageEvent)]
    [InlineData("BuyNftMarketplaceOfferMessageComposer", 2733, ClientMessages.BuyNftMarketplaceOfferMessageEvent)]
    [InlineData("GetNftMarketplaceItemStatsMessageComposer", 1364, ClientMessages.GetNftMarketplaceItemStatsMessageEvent)]
    public void November2026sNftRequestsAreReadAsTheServersOwn(string name, ushort header, ushort serverId)
    {
        Assert.Equal(header, ReleaseHeaders.Find(ClientReleases.November2026)!.Composers[name]);
        Assert.Equal(serverId, ServerHeaders.All.Composers[name]);
        Assert.True(ReleaseHeaderMap.For(ClientReleases.November2026)!.TryIncoming(header, out var incoming));
        Assert.Equal(serverId, incoming);
    }

    [Theory]
    [InlineData("NftMarketplaceMakeOfferResultMessageEvent", 2487, ServerMessages.NftMarketplaceMakeOfferResultMessageComposer)]
    [InlineData("NftMarketplaceBuyResultMessageEvent", 3940, ServerMessages.NftMarketplaceBuyResultMessageComposer)]
    [InlineData("NftMarketplaceItemStatsMessageEvent", 2153, ServerMessages.NftMarketplaceItemStatsMessageComposer)]
    public void November2026sNftAnswersGoOnlyToIt(string name, ushort header, ushort serverId)
    {
        Assert.Equal(serverId, ServerHeaders.All.Events[name]);
        Assert.True(ReleaseHeaderMap.For(ClientReleases.November2026)!.TryOutgoing(serverId, out var outgoing));
        Assert.Equal(header, outgoing);
        Assert.False(ReleaseHeaderMap.Native.TryOutgoing(serverId, out _));
        Assert.False(ReleaseHeaderMap.For(ClientReleases.October2026)!.TryOutgoing(serverId, out _));
    }

    [Fact]
    public void TheEmeraldBalanceGoesToEveryRelease()
    {
        Assert.True(ReleaseHeaderMap.For(ClientReleases.November2026)!.TryOutgoing(ServerMessages.EmeraldBalanceMessageComposer, out var november));
        Assert.Equal(1756, november);
        Assert.True(ReleaseHeaderMap.Native.TryOutgoing(ServerMessages.EmeraldBalanceMessageComposer, out _));
    }

    // Byte for byte as habbo.com sent them (WIN63-202610271147), recorded on 2026-11-03.

    [Fact]
    public void TheNftMarketplaceRulesAreHabbosOwn()
    {
        // habbo.com, 2026-11-03, MarketplaceConfiguration (66 bytes)
        var official = Convert.FromHexString("01000000010000000000000000000000020001869F000000A80000000700000002000186A000061A80"
                                             + "01000000020001869F00002760000000FA0000000200061A80");
        var ours = new MarketplaceConfigurationComposer(true, 1, 0, 0, 2, 99999, 168, 7, 2, 100000, 400000,
            NftMarketplaceEnabled: true, NftOfferMinPrice: 2, NftOfferMaxPrice: 99999, NftOfferDurationMinutes: 10080,
            NftMaximumOpenOffers: 250, NftSellingFeePercentage: 2, NftHalfTaxLimit: 400000);
        Assert.Equal(official, Payload(ours, ClientReleases.November2026));
    }

    [Fact]
    public void AnNftSearchRowIsAsHabbosWithItsNegativeRowId()
    {
        // habbo.com, 2026-11-03, the first row of the collectibles search
        var official = Convert.FromHexString("FFFFFFBF000000010000000500146E66745F736332355F62696762756E6E79626564"
                                             + "0000429F0000000100000014000003590000000F00000031");
        var row = new MarketplaceOfferData(-65, MarketplaceOfferData.Open, MarketplaceOfferData.NftItem, MarketplaceOfferData.NftItem,
            "nft_sc25_bigbunnybed", 17055, MarketplaceOfferData.FloorItem, null, null, null, null, null, 20, 857, 15, 49);
        var payload = Payload(new MarketPlaceOffersComposer([row], 299), ClientReleases.November2026);
        Assert.Equal(official, payload[4..^4]);
    }

    [Theory]
    // habbo.com, 2026-11-03: short of emeralds (at 20 and at 1), and a product it does not know
    [InlineData(NftMarketplaceBuyResultMessageComposer.NotEnoughEmeralds, "00134E4F545F454E4F5547485F454D4552414C4453000000000000000000000000")]
    [InlineData(NftMarketplaceBuyResultMessageComposer.Unknown, "0007554E4B4E4F574E000000000000000000000000")]
    public void ARefusedNftBuyIsItsResultWithNoOfferAndNoPrice(string result, string official) =>
        Assert.Equal(Convert.FromHexString(official), Payload(NftMarketplaceBuyResultMessageComposer.Refused(result), ClientReleases.November2026));

    [Fact]
    public void ARefusedNftOfferIsTwo() =>
        // habbo.com, 2026-11-03
        Assert.Equal(Convert.FromHexString("00000002"),
            Payload(new NftMarketplaceMakeOfferResultMessageComposer(NftMarketplaceMakeOfferResultMessageComposer.Failed), ClientReleases.November2026));

    [Fact]
    public void AnUnknownProductsStatsAreNothingOverThirtyDays() =>
        // habbo.com, 2026-11-03, for nft_does_not_exist and for the class name nft_a0club_sofa
        Assert.Equal(Convert.FromHexString("00000000000000000000001E000000000000000000000000"),
            Payload(new NftMarketplaceItemStatsMessageComposer(0, 0, 30, [], 0, 0), ClientReleases.November2026));

    [Fact]
    public void AProductsStatsAreLaidOutAsHabbos()
    {
        // habbo.com, 2026-11-03, nft_sc25_bigbunnybed
        var official = Convert.FromHexString("00000010000000310000001E00000007FFFFFFF90000000F00000001FFFFFFFA0000000F0000001D"
                                             + "FFFFFFFB0000000F00000013FFFFFFFC0000000A00000008FFFFFFFD0000001000000001FFFFFFFE00000011"
                                             + "00000005FFFFFFFF000000110000000A0000001400000014");
        var ours = new NftMarketplaceItemStatsMessageComposer(16, 49, 30,
        [
            new MarketplaceDayStatData(-7, 15, 1), new MarketplaceDayStatData(-6, 15, 29), new MarketplaceDayStatData(-5, 15, 19),
            new MarketplaceDayStatData(-4, 10, 8), new MarketplaceDayStatData(-3, 16, 1), new MarketplaceDayStatData(-2, 17, 5),
            new MarketplaceDayStatData(-1, 17, 10),
        ], 20, 20);
        Assert.Equal(official, Payload(ours, ClientReleases.November2026));
    }
}
