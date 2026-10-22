using System.Buffers.Binary;
using System.Text;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.IO;

/// <summary>Big-endian reader for Habbo message payloads (Flash wire format).
/// A class (not a ref struct) so it can cross async boundaries in message handlers.</summary>
public sealed class MessageReader(ReadOnlyMemory<byte> buffer)
{
    private readonly ReadOnlyMemory<byte> _buffer = buffer;
    private int _position;

    /// <summary>The release of the client that wrote the payload: a member a release added or dropped
    /// ([WireSince], [WireUntil]) is read only from the releases that send it.</summary>
    public string Release { get; init; } = ClientReleases.March2026;

    /// <summary>Whether the client has what <paramref name="release"/> added.</summary>
    public bool IsAtLeast(string release) => ClientReleases.IsAtLeast(Release, release);

    public int Remaining => _buffer.Length - _position;

    public byte ReadByte() => _buffer.Span[_position++];

    public bool ReadBoolean() => ReadByte() != 0;

    public short ReadShort()
    {
        var value = BinaryPrimitives.ReadInt16BigEndian(_buffer.Span[_position..]);
        _position += 2;
        return value;
    }

    public ushort ReadUShort()
    {
        var value = BinaryPrimitives.ReadUInt16BigEndian(_buffer.Span[_position..]);
        _position += 2;
        return value;
    }

    public int ReadInt()
    {
        var value = BinaryPrimitives.ReadInt32BigEndian(_buffer.Span[_position..]);
        _position += 4;
        return value;
    }

    /// <summary>Flash "long": two unsigned ints, high word first.</summary>
    public long ReadLong()
    {
        var hi = BinaryPrimitives.ReadUInt32BigEndian(_buffer.Span[_position..]);
        var lo = BinaryPrimitives.ReadUInt32BigEndian(_buffer.Span[(_position + 4)..]);
        _position += 8;
        return ((long)hi << 32) | lo;
    }

    public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt());

    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadLong());

    public string ReadString()
    {
        var length = ReadUShort();
        var value = Encoding.UTF8.GetString(_buffer.Span.Slice(_position, length));
        _position += length;
        return value;
    }

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        var value = _buffer.Span.Slice(_position, count);
        _position += count;
        return value;
    }
}
