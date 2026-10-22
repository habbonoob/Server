using System.Buffers;
using Habbo.Protocol.IO;

namespace Habbo.Protocol.Tests;

public class MessageIoRoundTripTests
{
    [Fact]
    public void AStringTooLongForItsLengthIsCutAndWhatFollowsStillReads()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessageWriter(buffer);
        writer.WriteString(new string('a', 65534) + "😀😀");
        writer.WriteInt(42);

        var reader = new MessageReader(buffer.WrittenMemory);
        var read = reader.ReadString();

        Assert.Equal(new string('a', 65534), read);
        Assert.Equal(42, reader.ReadInt());
    }

    [Fact]
    public void AllPrimitivesRoundTrip()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessageWriter(buffer);

        writer.WriteInt(-1234567);
        writer.WriteShort(-300);
        writer.WriteUShort(4000);
        writer.WriteByte(0xAB);
        writer.WriteBoolean(true);
        writer.WriteBoolean(false);
        writer.WriteLong(123456789012345L);
        writer.WriteLong(-42);
        writer.WriteFloat(3.5f);
        writer.WriteDouble(-2.25);
        writer.WriteString("Hello, Habbo!");
        writer.WriteString("ünïcødé ✓");
        writer.WriteBytes(new byte[] { 9, 8, 7 });

        var reader = new MessageReader(buffer.WrittenMemory);

        Assert.Equal(-1234567, reader.ReadInt());
        Assert.Equal(-300, reader.ReadShort());
        Assert.Equal(4000, reader.ReadUShort());
        Assert.Equal(0xAB, reader.ReadByte());
        Assert.True(reader.ReadBoolean());
        Assert.False(reader.ReadBoolean());
        Assert.Equal(123456789012345L, reader.ReadLong());
        Assert.Equal(-42, reader.ReadLong());
        Assert.Equal(3.5f, reader.ReadFloat());
        Assert.Equal(-2.25, reader.ReadDouble());
        Assert.Equal("Hello, Habbo!", reader.ReadString());
        Assert.Equal("ünïcødé ✓", reader.ReadString());
        Assert.Equal(new byte[] { 9, 8, 7 }, reader.ReadBytes(3).ToArray());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void WireBytesMatchFlashFormat()
    {
        // The client reads int32 BE; our writer must produce exactly that.
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessageWriter(buffer);
        writer.WriteInt(0x01020304);
        writer.WriteUShort(0x0506);
        writer.WriteString("AB");

        Assert.Equal(
            new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x00, 0x02, 0x41, 0x42 },
            buffer.WrittenSpan.ToArray());
    }
}
