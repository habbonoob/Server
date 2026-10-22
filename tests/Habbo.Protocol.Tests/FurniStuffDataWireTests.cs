using System.Buffers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Protocol.Tests;

/// <summary>Every layout the client's StuffDataFactory can pick, checked byte for byte against
/// the order its parser reads. A field too many or too few does not fail here on its own — it
/// shifts every item after this one in the packet — so each test ends by asserting nothing is
/// left over.</summary>
public class FurniStuffDataWireTests
{
    private static MessageReader Write(FurniDataView data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        FurniStuffData.From(data).Write(new MessageWriter(buffer));
        return new MessageReader(buffer.WrittenMemory);
    }

    private static FurniDataView Layout(int type) => FurniDataView.Plain with { Type = type, State = null };

    [Fact]
    public void LegacyIsTheHeaderAndOneString()
    {
        var reader = Write(FurniDataView.WithState("3"));

        Assert.Equal(FurniStuffData.LegacyType, reader.ReadInt());
        Assert.Equal("3", reader.ReadString());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void AnItemWithNothingRecordedStillWritesAnEmptyState()
    {
        // The client reads a string for the legacy layout regardless; omitting it would make
        // the next field of the packet its state.
        var reader = Write(FurniDataView.Plain);

        Assert.Equal(FurniStuffData.LegacyType, reader.ReadInt());
        Assert.Equal("", reader.ReadString());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void MapIsACountedListOfKeyValuePairs()
    {
        var reader = Write(Layout(FurniStuffData.MapType) with
        {
            MapEntries = [new("state", "1"), new("colour", "blue")],
        });

        Assert.Equal(FurniStuffData.MapType, reader.ReadInt());
        Assert.Equal(2, reader.ReadInt());
        Assert.Equal("state", reader.ReadString());
        Assert.Equal("1", reader.ReadString());
        Assert.Equal("colour", reader.ReadString());
        Assert.Equal("blue", reader.ReadString());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void StringArrayIsACountedListOfStrings()
    {
        var reader = Write(Layout(FurniStuffData.StringArrayType) with { StringValues = ["a", "b"] });

        Assert.Equal(FurniStuffData.StringArrayType, reader.ReadInt());
        Assert.Equal(2, reader.ReadInt());
        Assert.Equal("a", reader.ReadString());
        Assert.Equal("b", reader.ReadString());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void VoteResultIsAStateThenTheTally()
    {
        var reader = Write(Layout(FurniStuffData.VoteResultType) with { State = "0", VoteResult = 12 });

        Assert.Equal(FurniStuffData.VoteResultType, reader.ReadInt());
        Assert.Equal("0", reader.ReadString());
        Assert.Equal(12, reader.ReadInt());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void EmptyIsTheHeaderAlone()
    {
        var reader = Write(Layout(FurniStuffData.EmptyType));

        Assert.Equal(FurniStuffData.EmptyType, reader.ReadInt());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void IntArrayIsACountedListOfInts()
    {
        var reader = Write(Layout(FurniStuffData.IntArrayType) with { IntValues = [4, 8, 15] });

        Assert.Equal(FurniStuffData.IntArrayType, reader.ReadInt());
        Assert.Equal(3, reader.ReadInt());
        Assert.Equal(4, reader.ReadInt());
        Assert.Equal(8, reader.ReadInt());
        Assert.Equal(15, reader.ReadInt());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void HighScoreNestsTheNamesInsideEachRow()
    {
        var reader = Write(Layout(FurniStuffData.HighScoreType) with
        {
            State = "",
            ScoreType = 1,
            ClearType = 2,
            HighScoreEntries = [new(40, ["admin", "tester"], DateTimeOffset.UnixEpoch), new(10, ["solo"], DateTimeOffset.UnixEpoch)],
        });

        Assert.Equal(FurniStuffData.HighScoreType, reader.ReadInt());
        Assert.Equal("", reader.ReadString());
        Assert.Equal(1, reader.ReadInt());          // score type
        Assert.Equal(2, reader.ReadInt());          // clear type
        Assert.Equal(2, reader.ReadInt());          // rows

        Assert.Equal(40, reader.ReadInt());
        Assert.Equal(2, reader.ReadInt());
        Assert.Equal("admin", reader.ReadString());
        Assert.Equal("tester", reader.ReadString());

        Assert.Equal(10, reader.ReadInt());
        Assert.Equal(1, reader.ReadInt());
        Assert.Equal("solo", reader.ReadString());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void CrackableIsAStateThenHitsThenTarget()
    {
        var reader = Write(Layout(FurniStuffData.CrackableType) with { State = "2", Hits = 7, Target = 20 });

        Assert.Equal(FurniStuffData.CrackableType, reader.ReadInt());
        Assert.Equal("2", reader.ReadString());
        Assert.Equal(7, reader.ReadInt());
        Assert.Equal(20, reader.ReadInt());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void ALimitedEditionSetsTheFlagAndAppendsItsNumber()
    {
        var reader = Write(FurniDataView.WithState("0") with { UniqueSerialNumber = 7, UniqueSeriesSize = 100 });

        var header = reader.ReadInt();
        Assert.Equal(FurniStuffData.LegacyType, header & 0xFF);
        Assert.Equal(FurniStuffData.UniqueSerialNumberFlag, header & 0xFF00);
        Assert.Equal("0", reader.ReadString());
        Assert.Equal(7, reader.ReadInt());
        Assert.Equal(100, reader.ReadInt());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void ALimitedHighScoreNeverWritesTheTrailer()
    {
        // HighScoreStuffData is the one layout that does not chain to its base class, so it
        // never reads the serial trailer. Writing it would shift the rest of the packet.
        var reader = Write(Layout(FurniStuffData.HighScoreType) with
        {
            State = "",
            ScoreType = 0,
            ClearType = 0,
            HighScoreEntries = [],
            UniqueSerialNumber = 7,
            UniqueSeriesSize = 100,
        });

        var header = reader.ReadInt();
        Assert.Equal(FurniStuffData.HighScoreType, header & 0xFF);
        Assert.Equal("", reader.ReadString());
        Assert.Equal(0, reader.ReadInt());
        Assert.Equal(0, reader.ReadInt());
        Assert.Equal(0, reader.ReadInt());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void AnOrdinaryItemCarriesNoFlags()
    {
        var header = Write(FurniDataView.WithState("1")).ReadInt();
        Assert.Equal(0, header & 0xFF00);
    }
}
