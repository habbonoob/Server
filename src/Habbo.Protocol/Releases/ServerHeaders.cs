using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Releases;

/// <summary>The server's own message ids: one per message any known client release has, by the client's class name.
/// A message the native release has keeps its id there (the ids ClientMessages and ServerMessages hold); one a later
/// release added gets that release's id plus 10000 times the release's place after the native one (September 2026:
/// 1), which no client id reaches and which still points back at the release's own HabboMessages table.</summary>
public static class ServerHeaders
{
    /// <summary>What a later release's place is multiplied by: above any client id.</summary>
    public const int ReleaseStride = 10000;

    private static readonly Lazy<ReleaseHeaders> Table = new(Build);

    /// <summary>Every message's server id, by name: client composers (what the server receives), then events.</summary>
    public static ReleaseHeaders All => Table.Value;

    /// <summary>The server id of a message the release at <paramref name="place"/> after the native one added.</summary>
    public static ushort Later(int place, ushort header) => checked((ushort)(place * ReleaseStride + header));

    private static ReleaseHeaders Build()
    {
        var releases = ReleaseHeaders.All.Values
            .OrderBy(r => r.Release, Comparer<string>.Create(ClientReleases.Compare))
            .ToList();
        var native = releases.Single(r => r.Release == ReleaseHeaders.Native);
        var composers = new SortedDictionary<string, ushort>(native.Composers.ToDictionary(), StringComparer.Ordinal);
        var events = new SortedDictionary<string, ushort>(native.Events.ToDictionary(), StringComparer.Ordinal);

        var place = 0;
        foreach (var release in releases.SkipWhile(r => r != native).Skip(1))
        {
            place++;
            foreach (var (name, header) in release.Composers)
                composers.TryAdd(name, Later(place, header));
            foreach (var (name, header) in release.Events)
                events.TryAdd(name, Later(place, header));
        }

        return new ReleaseHeaders("server", composers, events);
    }
}
