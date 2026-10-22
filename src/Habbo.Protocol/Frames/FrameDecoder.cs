using System.Buffers;
using System.Buffers.Binary;
using Habbo.Protocol.Crypto;

namespace Habbo.Protocol.Frames;

/// <summary>Parses [int32BE length][int16BE header][payload] frames out of a byte stream.
/// A stream cipher may be attached after the handshake; cipher state only advances once a frame completes.</summary>
public sealed class FrameDecoder
{
    public const int MaxFrameLength = 262144;

    private StreamCipher? _cipher;

    public StreamCipher? Cipher { get => _cipher; set => _cipher = value; }

    /// <summary>Attempts to decode one frame from the front of <paramref name="input"/>.
    /// On success the frame is returned and input is advanced past it; on a partial frame,
    /// input is untouched (call with more bytes later).</summary>
    public bool TryDecode(ref ReadOnlySequence<byte> input, out GameFrame frame)
    {
        frame = default;
        if (input.Length < 6)
            return false;

        Span<byte> lengthBytes = stackalloc byte[4];
        CopyTo(input.Slice(0, 4), lengthBytes);
        var cipherSnapshot = _cipher?.SaveState();
        _cipher?.Process(lengthBytes);

        var length = BinaryPrimitives.ReadInt32BigEndian(lengthBytes);
        if (length < 2 || length > MaxFrameLength)
            throw new InvalidFrameException($"Invalid message length {length}.");

        if (input.Length < 4 + length)
        {
            if (cipherSnapshot is { } snapshot)
                _cipher!.RestoreState(snapshot);
            return false;
        }

        var frameBytes = ArrayPool<byte>.Shared.Rent(length);
        CopyTo(input.Slice(4, length), frameBytes.AsSpan(0, length));
        _cipher?.Process(frameBytes.AsSpan(0, length));

        var header = BinaryPrimitives.ReadUInt16BigEndian(frameBytes);
        frame = new GameFrame(header, frameBytes, length);
        input = input.Slice(4 + length);
        return true;
    }

    private static void CopyTo(in ReadOnlySequence<byte> source, Span<byte> destination)
    {
        if (source.Length > destination.Length)
            throw new InvalidFrameException("Frame larger than destination buffer.");

        foreach (var segment in source)
        {
            segment.Span.CopyTo(destination);
            destination = destination[segment.Length..];
        }
    }
}
