using System.Buffers;
using Habbo.Protocol.Composers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Incoming.Inventory.Trading;
using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Contracts;
using Habbo.Protocol.Messages.Outgoing.Availability;
using Habbo.Protocol.Messages.Outgoing.Handshake;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Protocol.Messages.Outgoing.Perk;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Furniture;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Tests;

/// <summary>Checks that a generated writer produces exactly the bytes the hand-written composer
/// did. Converting a message must not change a single byte.</summary>
public class GeneratedWireTests
{
    private static byte[] Payload(IComposer composer)
    {
        var buffer = new ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer));
        return buffer.WrittenSpan.ToArray();
    }

    [Fact]
    public void AListCountPastTheBytesLeftIsRefusedBeforeAnythingIsAllocated()
    {
        var payload = new ArrayBufferWriter<byte>();
        new MessageWriter(payload).WriteInt(0x7FFFFFC7);

        Assert.Throws<InvalidDataException>(() =>
            AddItemsToTradeEvent.Read(new MessageReader(payload.WrittenMemory)));
    }

    [Fact]
    public void AListCountPastWhatItsElementsCouldFillIsRefused()
    {
        // two ints claimed with one int's bytes and a bit left: more than a byte each, fewer than an element each
        var ints = new ArrayBufferWriter<byte>();
        var w = new MessageWriter(ints);
        w.WriteInt(2);
        w.WriteInt(5);
        w.WriteByte(0);

        Assert.Throws<InvalidDataException>(() => AddItemsToTradeEvent.Read(new MessageReader(ints.WrittenMemory)));

        // a nested list of structs: two nodes (a byte and an int each, at least) claimed with room for one
        var nested = new ArrayBufferWriter<byte>();
        w = new MessageWriter(nested);
        w.WriteInt(7);
        w.WriteShort(WiredUpdateContractMessageEvent.Trade);
        w.WriteBoolean(true);   // what the user gives
        w.WriteInt(1);          // one rule
        w.WriteInt(2);          // of two nodes
        w.WriteByte(TradeRequirementNodeData.Coins);
        w.WriteInt(5);
        w.WriteInt(0);

        Assert.Throws<InvalidDataException>(() =>
            WiredUpdateContractMessageEvent.Read(new MessageReader(nested.WrittenMemory)));
    }

    [Fact]
    public void AListOfAsManyAsAreSentIsRead()
    {
        var payload = new ArrayBufferWriter<byte>();
        var w = new MessageWriter(payload);
        w.WriteInt(2);
        w.WriteInt(5);
        w.WriteInt(6);

        var read = AddItemsToTradeEvent.Read(new MessageReader(payload.WrittenMemory));

        Assert.Equal([5, 6], read.ItemIds);
    }

    [Fact]
    public void AListCountsWithAnIntAndWritesElementsInTheRequestedWidth()
    {
        var generated = Payload(new AuthenticationOKMessageComposer(7, [1, 2, 3], 9));

        var expected = new ArrayBufferWriter<byte>();
        var w = new MessageWriter(expected);
        w.WriteInt(7);
        w.WriteInt(3);
        w.WriteShort(1);
        w.WriteShort(2);
        w.WriteShort(3);
        w.WriteInt(9);

        Assert.Equal(expected.WrittenSpan.ToArray(), generated);
    }

    [Fact]
    public void AListOfStructsWritesEachStructInline()
    {
        var generated = Payload(new PerkAllowancesMessageComposer(
        [
            new PerkAllowanceData("CITIZEN", string.Empty, true),
            new PerkAllowanceData("CAMERA", "no", false),
        ]));

        var expected = new ArrayBufferWriter<byte>();
        var w = new MessageWriter(expected);
        w.WriteInt(2);
        w.WriteString("CITIZEN");
        w.WriteString(string.Empty);
        w.WriteBoolean(true);
        w.WriteString("CAMERA");
        w.WriteString("no");
        w.WriteBoolean(false);

        Assert.Equal(expected.WrittenSpan.ToArray(), generated);
    }

    [Fact]
    public void ACountWrittenBeforeAnotherMemberLeavesTheItemsInPlace()
    {
        var generated = Payload(new RoomDimmerPresetsMessageComposer(
            2, [new RoomDimmerPresetData(1, 2, "#FF0000", 200), new RoomDimmerPresetData(2, 1, "#000000", 80)], true, 55));

        var expected = new ArrayBufferWriter<byte>();
        var w = new MessageWriter(expected);
        w.WriteInt(2);
        w.WriteInt(2);
        w.WriteInt(1);
        w.WriteInt(2);
        w.WriteString("#FF0000");
        w.WriteInt(200);
        w.WriteInt(2);
        w.WriteInt(1);
        w.WriteString("#000000");
        w.WriteInt(80);
        w.WriteBoolean(true);
        w.WriteInt(55);

        Assert.Equal(expected.WrittenSpan.ToArray(), generated);
    }

    [Fact]
    public void ATrailingPartUnderAFalseConditionIsLeftOff()
    {
        var alone = Payload(new SlideObjectBundleMessageComposer(1, 2, 1, 3, [new SlideObjectData(9, "0.0", "0.5")], 4, null, null));
        var ridden = Payload(new SlideObjectBundleMessageComposer(1, 2, 1, 3, [], 4,
            SlideObjectBundleMessageComposer.AvatarSlides, new SlideObjectData(0, "1.0", "1.0")));

        var expected = new ArrayBufferWriter<byte>();
        var w = new MessageWriter(expected);
        w.WriteInt(1);
        w.WriteInt(2);
        w.WriteInt(1);
        w.WriteInt(3);
        w.WriteInt(1);
        w.WriteInt(9);
        w.WriteString("0.0");
        w.WriteString("0.5");
        w.WriteInt(4);
        Assert.Equal(expected.WrittenSpan.ToArray(), alone);

        var expectedRidden = new ArrayBufferWriter<byte>();
        w = new MessageWriter(expectedRidden);
        w.WriteInt(1);
        w.WriteInt(2);
        w.WriteInt(1);
        w.WriteInt(3);
        w.WriteInt(0);
        w.WriteInt(4);
        w.WriteInt(SlideObjectBundleMessageComposer.AvatarSlides);
        w.WriteInt(0);
        w.WriteString("1.0");
        w.WriteString("1.0");
        Assert.Equal(expectedRidden.WrittenSpan.ToArray(), ridden);
    }

    [Fact]
    public void AnIntegerStringIsWrittenAsPlainText()
    {
        var generated = Payload(new CreditBalanceComposer(1234));

        var expected = new ArrayBufferWriter<byte>();
        new MessageWriter(expected).WriteString("1234");

        Assert.Equal(expected.WrittenSpan.ToArray(), generated);
    }

    [Fact]
    public void HeadersComeFromTheAttribute()
    {
        Assert.Equal(ServerMessages.AuthenticationOKMessageComposer, new AuthenticationOKMessageComposer(0, [], 0).Header);
        Assert.Equal(ServerMessages.GenericErrorComposer, new GenericErrorComposer(0).Header);
        Assert.Equal(ServerMessages.NavigatorSettingsComposer, new NavigatorSettingsComposer(0, 0).Header);
        Assert.Equal(ServerMessages.FurnitureAliasesMessageComposer, new FurnitureAliasesMessageComposer([]).Header);
        Assert.Equal(ServerMessages.AvailabilityStatusMessageComposer,
            new AvailabilityStatusMessageComposer(true, false, true).Header);
    }

    [Fact]
    public void TheManifestDescribesEveryGeneratedMessage()
    {
        Assert.Contains("\"header\":3847", WireManifest.Json);
        Assert.Contains("\"name\":\"AuthenticationOKMessageComposer\"", WireManifest.Json);

        // the element width the client reads shows up in the manifest, not just in the code
        Assert.Contains("\"element\":\"i16\"", WireManifest.Json);
    }

    [Fact]
    public void HeightsAreWrittenTheWayTheClientParsesThem()
    {
        Assert.Equal("1.0", WireFormatting.Height(1));
        Assert.Equal("0.0", WireFormatting.Height(0));
        Assert.Equal("2.5", WireFormatting.Height(2.5));
        Assert.Equal("1234", WireFormatting.Integer(1234));
        Assert.Equal("ff", WireFormatting.Hex(255));
    }
}
