using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.IO;

/// <summary>Big-endian writer for Habbo message payloads (Flash wire format), as a client of one release reads
/// them: a member a release added or dropped ([WireSince], [WireUntil]) is written only to the releases that have it.</summary>
public ref struct MessageWriter
{
    private readonly IBufferWriter<byte> _output;
    private readonly string? _release;

    public MessageWriter(IBufferWriter<byte> output) => _output = output;

    public MessageWriter(IBufferWriter<byte> output, string release)
    {
        _output = output;
        _release = release;
    }

    /// <summary>The client release written for; the native one unless given.</summary>
    public readonly string Release => _release ?? ClientReleases.March2026;

    /// <summary>Whether the client has what <paramref name="release"/> added.</summary>
    public readonly bool IsAtLeast(string release) => ClientReleases.IsAtLeast(Release, release);

    public void WriteByte(byte value)
    {
        var span = _output.GetSpan(1);
        span[0] = value;
        _output.Advance(1);
    }

    public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    public void WriteShort(short value)
    {
        var span = _output.GetSpan(2);
        BinaryPrimitives.WriteInt16BigEndian(span, value);
        _output.Advance(2);
    }

    public void WriteUShort(ushort value)
    {
        var span = _output.GetSpan(2);
        BinaryPrimitives.WriteUInt16BigEndian(span, value);
        _output.Advance(2);
    }

    public void WriteInt(int value)
    {
        var span = _output.GetSpan(4);
        BinaryPrimitives.WriteInt32BigEndian(span, value);
        _output.Advance(4);
    }

    /// <summary>Flash "long": two unsigned ints, high word first.</summary>
    public void WriteLong(long value)
    {
        var span = _output.GetSpan(8);
        BinaryPrimitives.WriteUInt32BigEndian(span, (uint)(value >> 32));
        BinaryPrimitives.WriteUInt32BigEndian(span[4..], (uint)value);
        _output.Advance(8);
    }

    public void WriteFloat(float value) => WriteInt(BitConverter.SingleToInt32Bits(value));

    public void WriteDouble(double value) => WriteLong(BitConverter.DoubleToInt64Bits(value));

    /// <summary>A string as the client reads one: its UTF-8 length in two bytes, then the bytes. One longer than two
    /// bytes can count is cut at the last whole character that fits; written whole, its length would wrap and the
    /// client would read the rest as the fields after it, and every frame after that.</summary>
    public void WriteString(string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > ushort.MaxValue)
        {
            value = Fitting(value, ushort.MaxValue);
            byteCount = Encoding.UTF8.GetByteCount(value);
        }

        WriteUShort((ushort)byteCount);

        var span = _output.GetSpan(byteCount);
        Encoding.UTF8.GetBytes(value, span);
        _output.Advance(byteCount);
    }

    /// <summary>The longest start of the text whose UTF-8 takes no more than the bytes given, never half a pair.</summary>
    private static string Fitting(string value, int maxBytes)
    {
        var (bytes, length) = (0, 0);
        while (length < value.Length)
        {
            var width = char.IsHighSurrogate(value[length]) && length + 1 < value.Length ? 2 : 1;
            var size = Encoding.UTF8.GetByteCount(value.AsSpan(length, width));
            if (bytes + size > maxBytes)
                break;
            (bytes, length) = (bytes + size, length + width);
        }

        return value[..length];
    }

    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        var span = _output.GetSpan(value.Length);
        value.CopyTo(span);
        _output.Advance(value.Length);
    }
}
