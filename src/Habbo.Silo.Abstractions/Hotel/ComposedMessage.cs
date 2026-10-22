using Orleans;

namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>A message ready to send: its native header and payload, and the payload each other client release reads
/// it as, for a message whose layout changed between releases (only those whose bytes differ).</summary>
[GenerateSerializer, Immutable]
public sealed record ComposedMessage(
    [property: Id(0)] ushort Header,
    [property: Id(1)] byte[] Payload,
    [property: Id(2)] Dictionary<string, byte[]>? Releases = null)
{
    /// <summary>The payload as a client of this release reads it.</summary>
    public byte[] PayloadFor(string release) =>
        Releases is not null && Releases.TryGetValue(release, out var payload) ? payload : Payload;
}
