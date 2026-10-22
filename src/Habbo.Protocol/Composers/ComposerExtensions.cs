using Habbo.Protocol.IO;
using Habbo.Protocol.Releases;
using Habbo.Silo.Abstractions.Hotel;

namespace Habbo.Protocol.Composers;

public static class ComposerExtensions
{
    /// <summary>Serializes the message into a buffer and returns the payload bytes, as the native release reads them.</summary>
    public static byte[] ToPayload(this IComposer composer) => composer.ToPayload(ReleaseHeaders.Native);

    /// <summary>Serializes the message as a client of <paramref name="release"/> reads it.</summary>
    public static byte[] ToPayload(this IComposer composer, string release)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        composer.Compose(new MessageWriter(buffer, release));
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>The message ready to send to any client: the native payload, and for a message whose layout varies
    /// by release, the payload of each other release the server has headers for whose bytes differ.</summary>
    public static ComposedMessage ToMessage(this IComposer composer)
    {
        var payload = composer.ToPayload();
        if (!composer.VariesByRelease)
            return new ComposedMessage(composer.Header, payload);

        Dictionary<string, byte[]>? releases = null;
        foreach (var release in ReleaseHeaders.All.Keys.Where(r => r != ReleaseHeaders.Native))
        {
            var other = composer.ToPayload(release);
            if (!other.AsSpan().SequenceEqual(payload))
                (releases ??= new Dictionary<string, byte[]>(StringComparer.Ordinal))[release] = other;
        }

        return new ComposedMessage(composer.Header, payload, releases);
    }
}
