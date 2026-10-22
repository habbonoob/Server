namespace Habbo.CatalogDumper;

/// <summary>Gives a hidden catalog page an id.
///
/// These pages do have ids on Habbo's side — they are ordinary pages that happen to be disabled
/// or hidden, and they group the pages beneath them. Habbo simply sends -1 in place of the id
/// for anything the client may not open, so the real id never crosses the wire and we cannot
/// know it.
///
/// What does cross the wire is the name, and that is unique across the whole catalog: in a US
/// dump, 178 pages had 178 distinct names but only 163 distinct ids. The name is therefore the
/// page's usable identity here, and the number is derived from it.
///
/// Deriving rather than counting as we go means the same catalog always produces the same ids,
/// so a re-dump does not renumber everything because Habbo hid one more page near the top.</summary>
public static class HiddenPageIdentity
{
    /// <summary>What Habbo sends in place of the id of a page the client may not open.</summary>
    public const int HiddenPageId = -1;

    /// <summary>How far below -1 a derived id may reach. Habbo's real page ids are positive and
    /// -1 is its own marker, so this range collides with neither.</summary>
    private const uint Range = 1_000_000_000;

    /// <param name="used">Ids already handed out, so two pages cannot land on one row.</param>
    public static int Derive(string name, HashSet<int> used)
    {
        var id = -2 - (int)(Fnv1a(name) % Range);

        // Two names hashing together is vanishingly unlikely at this size, but stepping down
        // keeps it deterministic rather than merging two pages into a single row.
        while (!used.Add(id))
            id--;

        return id;
    }

    /// <summary>FNV-1a, rather than string.GetHashCode, which is randomised per process and
    /// would hand out a different id on every run.</summary>
    private static uint Fnv1a(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var c in value)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }
}
