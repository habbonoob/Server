using System.Buffers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Incoming.Room.Furniture;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;

namespace Habbo.Protocol.Tests;

/// <summary>The recolour window's messages, in the order the client writes and reads them.</summary>
public class FurniRecolorWireTests
{
    [Fact]
    public void TheApplyIsTheFurniTheScopeAndEachChannel()
    {
        // FurniRecolorMessageComposer: [furniId, scope, color >> 16 & 0xFF, color >> 8 & 0xFF, color & 0xFF]
        var buffer = new ArrayBufferWriter<byte>();
        var w = new MessageWriter(buffer);
        foreach (var value in new[] { 42, FurniRecolorMessageEvent.SameType, 0xAE, 0xF0, 0xD8 })
            w.WriteInt(value);
        var r = new MessageReader(buffer.WrittenMemory);

        Assert.Equal(new FurniRecolorMessageEvent(42, FurniRecolorMessageEvent.SameType, 0xAE, 0xF0, 0xD8), FurniRecolorMessageEvent.Read(r));
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void AnUpdateIsItsFurniThenEachChannelAsAShort()
    {
        // FurniRecolorUpdateMessageParser: updates, each RecolorUpdateData: ids, then red, green and blue as shorts
        var buffer = new ArrayBufferWriter<byte>();
        new FurniRecolorUpdateMessageComposer([new RecolorUpdateData([7, 8], 0xAE, 0xF0, 0xD8)]).Compose(new MessageWriter(buffer));
        var r = new MessageReader(buffer.WrittenMemory);

        Assert.Equal(1, r.ReadInt());
        Assert.Equal(2, r.ReadInt());
        Assert.Equal((7, 8), (r.ReadInt(), r.ReadInt()));
        Assert.Equal(((short)0xAE, (short)0xF0, (short)0xD8), (r.ReadShort(), r.ReadShort(), r.ReadShort()));
        Assert.Equal(0, r.Remaining);
    }
}
