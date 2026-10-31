using System.Buffers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

namespace Habbo.Protocol.Tests;

/// <summary>The client writes a numeric id it keyed a Dictionary by (or got as a number) as an int where the layout
/// has a string: these messages read either.</summary>
public class ClientNumericIdTests
{
    private static MessageReader Payload(Action<MessageWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        write(new MessageWriter(buffer));
        return new MessageReader(buffer.WrittenSpan.ToArray());
    }

    [Fact]
    public void KnownVariablesComeAsInts()
    {
        var message = WiredGetAllVariablesDiffsMessageEvent.Read(Payload(w =>
        {
            w.WriteInt(3);
            w.WriteInt(-120); w.WriteInt(11);
            w.WriteInt(-230); w.WriteInt(22);
            w.WriteInt(70001); w.WriteInt(33);
        }));

        Assert.Equal([new("-120", 11), new("-230", 22), new("70001", 33)], message.Known);
    }

    [Fact]
    public void KnownVariablesComeAsStringsOrMixed()
    {
        var strings = WiredGetAllVariablesDiffsMessageEvent.Read(Payload(w =>
        {
            w.WriteInt(2);
            w.WriteString("-120"); w.WriteInt(11);
            w.WriteString("1234.level"); w.WriteInt(22);
        }));
        var mixed = WiredGetAllVariablesDiffsMessageEvent.Read(Payload(w =>
        {
            w.WriteInt(4);
            w.WriteInt(1234); w.WriteInt(11);
            w.WriteString("1234.level"); w.WriteInt(22);
            w.WriteString("~146"); w.WriteInt(-5);
            w.WriteInt(-100); w.WriteInt(33);
        }));

        Assert.Equal([new("-120", 11), new("1234.level", 22)], strings.Known);
        Assert.Equal([new("1234", 11), new("1234.level", 22), new("~146", -5), new("-100", 33)], mixed.Known);
    }

    [Fact]
    public void NoKnownVariablesIsAnEmptyList()
    {
        Assert.Empty(WiredGetAllVariablesDiffsMessageEvent.Read(Payload(w => w.WriteInt(0))).Known);
    }

    [Fact]
    public void APhotoIdComesAsAStringOrAnInt()
    {
        void Rest(MessageWriter w)
        {
            w.WriteInt(3); w.WriteInt(7); w.WriteInt(12); w.WriteInt(0);
            w.WriteString(""); w.WriteString("");
        }

        var asString = CallForHelpFromPhotoMessageEvent.Read(Payload(w => { w.WriteString("4821"); Rest(w); }));
        var asInt = CallForHelpFromPhotoMessageEvent.Read(Payload(w => { w.WriteInt(4821); Rest(w); }));

        Assert.Equal(new CallForHelpFromPhotoMessageEvent("4821", 3, 7, 12, 0, "", ""), asString);
        Assert.Equal(asString, asInt);
    }
}
