namespace Habbo.Protocol.Releases;

/// <summary>Translates one connection's headers between its client's release and the server's own ids
/// (<see cref="ServerHeaders"/>), by message name. A message the client's release lacks has no counterpart: the
/// server cannot read it from the client, and must not send it, since the client would read the frame as whatever
/// message that id is in its own release.</summary>
public sealed class ReleaseHeaderMap
{
    private readonly IReadOnlyDictionary<ushort, ushort> _incoming;
    private readonly IReadOnlyDictionary<ushort, ushort> _outgoing;

    private static readonly Lazy<ReleaseHeaderMap> NativeMap =
        new(() => Between(ReleaseHeaders.Find(ReleaseHeaders.Native)!, ServerHeaders.All));

    /// <summary>The native release: its headers are the server's own, but what later releases added is not its.</summary>
    public static ReleaseHeaderMap Native => NativeMap.Value;

    public string Release { get; }

    private ReleaseHeaderMap(string release, IReadOnlyDictionary<ushort, ushort> incoming, IReadOnlyDictionary<ushort, ushort> outgoing)
    {
        Release = release;
        _incoming = incoming;
        _outgoing = outgoing;
    }

    /// <summary>The map for a release, or null when the server has no table for it.</summary>
    public static ReleaseHeaderMap? For(string release)
    {
        if (release == ReleaseHeaders.Native)
            return Native;

        return ReleaseHeaders.Find(release) is { } client ? Between(client, ServerHeaders.All) : null;
    }

    public static ReleaseHeaderMap Between(ReleaseHeaders client, ReleaseHeaders server)
        => new(client.Release, Pair(client.Composers, server.Composers), Pair(server.Events, client.Events));

    /// <summary>A header the client sent, as the server's id of the same message.</summary>
    public bool TryIncoming(ushort clientHeader, out ushort serverHeader)
        => Translate(_incoming, clientHeader, out serverHeader);

    /// <summary>A server id the server sends, as the client's header of the same message.</summary>
    public bool TryOutgoing(ushort serverHeader, out ushort clientHeader)
        => Translate(_outgoing, serverHeader, out clientHeader);

    private static bool Translate(IReadOnlyDictionary<ushort, ushort> map, ushort header, out ushort translated)
        => map.TryGetValue(header, out translated);

    /// <summary>from's header → to's header, for every name both have.</summary>
    private static Dictionary<ushort, ushort> Pair(IReadOnlyDictionary<string, ushort> from, IReadOnlyDictionary<string, ushort> to)
        => from.Where(f => to.ContainsKey(f.Key)).ToDictionary(f => f.Value, f => to[f.Key]);
}
