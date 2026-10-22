using System.Buffers;
using Habbo.Protocol.Composers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Outgoing.Catalog;

namespace Habbo.Protocol.Tests;

/// <summary>The catalog is the first place a layout branches on a value the client has already
/// read, so these check the branches byte for byte against the client's own parsers.</summary>
public class CatalogWireTests
{
    private static MessageReader Compose(IComposer composer)
    {
        var buffer = new ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer));
        return new MessageReader(buffer.WrittenMemory);
    }

    private static byte[] Body(IComposer composer)
    {
        var buffer = new ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer));
        return buffer.WrittenSpan.ToArray();
    }

    private static byte[] Bytes(CatalogProductData product)
    {
        var buffer = new ArrayBufferWriter<byte>();
        product.Write(new MessageWriter(buffer));
        return buffer.WrittenSpan.ToArray();
    }

    [Fact]
    public void AFurnitureProductWritesTheFullLayout()
    {
        // CatalogPageMessageProductData: type, classId, extraParam, count, limited flag.
        var reader = new MessageReader(Bytes(CatalogProductData.Furniture("s", 26, 2)));

        Assert.Equal("s", reader.ReadString());
        Assert.Equal(26, reader.ReadInt());
        Assert.Equal("", reader.ReadString());
        Assert.Equal(2, reader.ReadInt());
        Assert.False(reader.ReadBoolean());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void ABadgeProductStopsAfterItsCode()
    {
        // The client's parser branches on "b" and reads only the code, assuming a count of 1.
        // Writing the furniture fields here would desync every product after it.
        var reader = new MessageReader(Bytes(CatalogProductData.Badge("ACH_Login1")));

        Assert.Equal("b", reader.ReadString());
        Assert.Equal("ACH_Login1", reader.ReadString());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void ALimitedProductWritesItsSeriesTrailerOnlyWhenLimited()
    {
        var limited = new CatalogProductData(
            "s", FurniClassId: 26, ExtraParam: "", ProductCount: 1,
            UniqueLimitedItem: true, UniqueLimitedItemSeriesSize: 100, UniqueLimitedItemsLeft: 42);

        var reader = new MessageReader(Bytes(limited));

        Assert.Equal("s", reader.ReadString());
        Assert.Equal(26, reader.ReadInt());
        Assert.Equal("", reader.ReadString());
        Assert.Equal(1, reader.ReadInt());
        Assert.True(reader.ReadBoolean());
        Assert.Equal(100, reader.ReadInt());
        Assert.Equal(42, reader.ReadInt());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void TheIndexWritesTheRootNodeInlineAndNestsItsChildren()
    {
        // NodeData is recursive: the client reads one root, then each child the same way.
        var child = new CatalogNodeData(
            Visible: true, Icon: 8, PageId: 10, PageName: "seating", Localization: "Seating",
            OfferIds: [100, 101], Children: []);

        var root = new CatalogNodeData(
            Visible: true, Icon: 0, PageId: 1, PageName: "root", Localization: "",
            OfferIds: [], Children: [child]);

        var reader = Compose(new CatalogIndexMessageComposer(root, NewAdditionsAvailable: false, "NORMAL"));

        Assert.True(reader.ReadBoolean());
        Assert.Equal(0, reader.ReadInt());
        Assert.Equal(1, reader.ReadInt());
        Assert.Equal("root", reader.ReadString());
        Assert.Equal("", reader.ReadString());
        Assert.Equal(0, reader.ReadInt());   // no offer ids
        Assert.Equal(1, reader.ReadInt());   // one child

        Assert.True(reader.ReadBoolean());
        Assert.Equal(8, reader.ReadInt());
        Assert.Equal(10, reader.ReadInt());
        Assert.Equal("seating", reader.ReadString());
        Assert.Equal("Seating", reader.ReadString());
        Assert.Equal(2, reader.ReadInt());
        Assert.Equal(100, reader.ReadInt());
        Assert.Equal(101, reader.ReadInt());
        Assert.Equal(0, reader.ReadInt());   // no grandchildren

        // The tree ends before the two trailing fields, which are the message's own.
        Assert.False(reader.ReadBoolean());
        Assert.Equal("NORMAL", reader.ReadString());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void ThePurchaseConfirmationOmitsTheSilverPriceAndPreviewImage()
    {
        // PurchaseOKMessageOfferData is two fields shorter than the page's offer; reusing the
        // page struct here would leave the client reading the products as a price.
        var offer = CatalogOfferData.ForCredits(
            100, "chair_silo", priceInCredits: 5, [CatalogProductData.Furniture("s", 26, 1)]);

        var reader = Compose(new PurchaseOKMessageComposer(PurchaseOfferData.From(offer), []));

        Assert.Equal(100, reader.ReadInt());
        Assert.Equal("chair_silo", reader.ReadString());
        Assert.False(reader.ReadBoolean());  // isRent
        Assert.Equal(5, reader.ReadInt());   // credits
        Assert.Equal(0, reader.ReadInt());   // activity points
        Assert.Equal(0, reader.ReadInt());   // activity point type
        Assert.False(reader.ReadBoolean());  // giftable — no silver price before it
        Assert.Equal(1, reader.ReadInt());   // one product
    }

    [Fact]
    public void ThePurchaseConfirmationMatchesHabbosBytes()
    {
        // Habbo.com buying a Pineapple Plant (offer 143, class 160), 2026-10-10: the offer, then the furni handed over.
        var offer = new PurchaseOfferData(143, "A1 ANANAS", false, 3, 0, 0, true,
            [CatalogProductData.Furniture("s", 160, 1)], 0, true);

        var bytes = Convert.ToHexString(Body(new PurchaseOKMessageComposer(offer, [923650708])));

        Assert.Equal("0000008F0009413120414E414E4153000000000300000000000000000100000001000173000000A000000000000100000000000100000001370DCA94", bytes);
    }
}
