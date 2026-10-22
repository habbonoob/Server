using System.Buffers;
using System.Buffers.Binary;
using Habbo.Protocol.Crypto;
using Habbo.Protocol.Frames;

namespace Habbo.Protocol.Tests;

public class FrameCodecTests
{
    [Fact]
    public void RoundTripWithoutCipher()
    {
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        var buffer = new ArrayBufferWriter<byte>();
        FrameEncoder.Encode(buffer, 4000, payload, null);

        var sequence = new ReadOnlySequence<byte>(buffer.WrittenMemory);
        var decoder = new FrameDecoder();

        Assert.True(decoder.TryDecode(ref sequence, out var frame));
        using (frame)
        {
            Assert.Equal(4000, frame.Header);
            Assert.Equal(payload, frame.Payload.ToArray());
        }
        Assert.Equal(0, sequence.Length);
    }

    [Fact]
    public void EmptyPayloadFrame()
    {
        var buffer = new ArrayBufferWriter<byte>();
        FrameEncoder.Encode(buffer, 2735, ReadOnlySpan<byte>.Empty, null);

        var sequence = new ReadOnlySequence<byte>(buffer.WrittenMemory);
        var decoder = new FrameDecoder();

        Assert.True(decoder.TryDecode(ref sequence, out var frame));
        using (frame)
        {
            Assert.Equal(2735, frame.Header);
            Assert.Empty(frame.Payload.ToArray());
        }
    }

    [Fact]
    public void FrameDeliveredOneByteAtATimeWithCipher()
    {
        var key = "handshake-key"u8;
        var writerCipher = new Rc4(key);
        var buffer = new ArrayBufferWriter<byte>();
        FrameEncoder.Encode(buffer, 1142, new byte[] { 0xAA, 0xBB }, writerCipher);
        FrameEncoder.Encode(buffer, 1038, new byte[] { 0x01 }, writerCipher);

        // Feed the encoded bytes one at a time, mirroring the real read loop:
        // accumulate into a growing buffer and retry decoding after each byte.
        var received = new ArrayBufferWriter<byte>();
        var decoder = new FrameDecoder { Cipher = new Rc4(key) };
        var decoded = new List<GameFrame>();
        var consumed = 0;

        foreach (var b in buffer.WrittenSpan)
        {
            var one = received.GetSpan(1);
            one[0] = b;
            received.Advance(1);

            var sequence = new ReadOnlySequence<byte>(received.WrittenMemory[consumed..]);
            while (decoder.TryDecode(ref sequence, out var frame))
                decoded.Add(frame);
            consumed = received.WrittenCount - (int)sequence.Length;
        }

        Assert.Equal(2, decoded.Count);
        Assert.Equal(1142, decoded[0].Header);
        Assert.Equal(new byte[] { 0xAA, 0xBB }, decoded[0].Payload.ToArray());
        Assert.Equal(1038, decoded[1].Header);
        Assert.Equal(new byte[] { 0x01 }, decoded[1].Payload.ToArray());

        decoded.ForEach(f => f.Dispose());
    }

    [Fact]
    public void InvalidLengthThrows()
    {
        Span<byte> bytes = stackalloc byte[6];
        BinaryPrimitives.WriteInt32BigEndian(bytes, 300000);
        BinaryPrimitives.WriteUInt16BigEndian(bytes[4..], 1);

        var sequence = new ReadOnlySequence<byte>(bytes.ToArray());
        var decoder = new FrameDecoder();

        Assert.Throws<InvalidFrameException>(() => decoder.TryDecode(ref sequence, out _));
    }

    [Fact]
    public void ZeroLengthThrows()
    {
        Span<byte> bytes = stackalloc byte[6];
        BinaryPrimitives.WriteInt32BigEndian(bytes, 1);
        BinaryPrimitives.WriteUInt16BigEndian(bytes[4..], 1);

        var sequence = new ReadOnlySequence<byte>(bytes.ToArray());
        var decoder = new FrameDecoder();

        Assert.Throws<InvalidFrameException>(() => decoder.TryDecode(ref sequence, out _));
    }

    [Fact]
    public void MultipleFramesInOneBuffer()
    {
        var buffer = new ArrayBufferWriter<byte>();
        FrameEncoder.Encode(buffer, 100, new byte[] { 1 }, null);
        FrameEncoder.Encode(buffer, 200, new byte[] { 2, 3 }, null);
        FrameEncoder.Encode(buffer, 300, ReadOnlySpan<byte>.Empty, null);

        var sequence = new ReadOnlySequence<byte>(buffer.WrittenMemory);
        var decoder = new FrameDecoder();

        Assert.True(decoder.TryDecode(ref sequence, out var first));
        Assert.True(decoder.TryDecode(ref sequence, out var second));
        Assert.True(decoder.TryDecode(ref sequence, out var third));
        Assert.False(decoder.TryDecode(ref sequence, out _));

        using (first) { Assert.Equal(100, first.Header); }
        using (second) { Assert.Equal(new byte[] { 2, 3 }, second.Payload.ToArray()); }
        using (third) { Assert.Equal(300, third.Header); }
    }
}
