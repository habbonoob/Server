namespace Habbo.Protocol.Wire;

/// <summary>The client releases whose wire layouts the server knows, oldest first, and their order. A release is
/// named WIN63-&lt;build time&gt;-&lt;build&gt;; later builds are later releases.</summary>
public static class ClientReleases
{
    /// <summary>The release the server was written against: ClientMessages and ServerMessages hold its ids, and every
    /// layout without [WireSince] or [WireUntil] is its layout.</summary>
    public const string March2026 = "WIN63-202603212315-320263584";

    public const string September2026 = "WIN63-202609161723-93809945";

    public const string October2026 = "WIN63-202610131318-860371703";

    /// <summary>Orders two releases by build time; names that carry none are ordered as text.</summary>
    public static int Compare(string a, string b)
    {
        if (a == b)
            return 0;

        return BuildTime(a) is { } x && BuildTime(b) is { } y && x != y ? x.CompareTo(y) : string.CompareOrdinal(a, b);
    }

    /// <summary>Whether a client of <paramref name="release"/> has what <paramref name="since"/> added.</summary>
    public static bool IsAtLeast(string release, string since) => Compare(release, since) >= 0;

    /// <summary>The build time between the name's first and second dash. Read in place: it is asked for every gated
    /// field of every message composed for each release, and splitting the name each time made garbage of it.</summary>
    private static long? BuildTime(string release)
    {
        var rest = release.AsSpan();
        var first = rest.IndexOf('-');
        if (first < 0)
            return null;

        rest = rest[(first + 1)..];
        var second = rest.IndexOf('-');
        return long.TryParse(second < 0 ? rest : rest[..second], out var time) ? time : null;
    }
}
