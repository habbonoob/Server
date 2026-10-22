using System.Buffers;
using System.Buffers.Binary;
using Habbo.Protocol.Crypto;
using Habbo.Protocol.IO;

namespace Habbo.Protocol.Frames;

public static class FrameEncoder
{
    /// <summary>Writes one complete frame into the output buffer, ciphering length+header+payload in stream order.</summary>
    public static void Encode(IBufferWriter<byte> output, ushort header, ReadOnlySpan<byte> payload, StreamCipher? cipher)
    {
        var length = payload.Length + 2;

        Span<byte> prefix = stackalloc byte[6];
        BinaryPrimitives.WriteInt32BigEndian(prefix, length);
        BinaryPrimitives.WriteUInt16BigEndian(prefix[4..], header);
        cipher?.Process(prefix);
        output.Write(prefix);

        if (payload.Length == 0)
            return;

        var span = output.GetSpan(payload.Length);
        payload.CopyTo(span);
        cipher?.Process(span[..payload.Length]);
        output.Advance(payload.Length);
    }

    /// <summary>Composes a payload with a writer, then encodes it as one frame.</summary>
    public static void Encode(IBufferWriter<byte> output, ushort header, Action<MessageWriter> compose, StreamCipher? cipher)
    {
        var payloadBuffer = new ArrayBufferWriter<byte>();
        compose(new MessageWriter(payloadBuffer));
        Encode(output, header, payloadBuffer.WrittenSpan, cipher);
    }
}
