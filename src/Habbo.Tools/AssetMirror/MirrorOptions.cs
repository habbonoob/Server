namespace Habbo.Tools.AssetMirror;

/// <summary>What to mirror, from which hotel, into where, and how hard to go at it.</summary>
public sealed record MirrorOptions(
    string Out,
    string Hotel,
    IReadOnlySet<AssetKind> Kinds,
    int Workers,
    int Limit,
    int MaxSample,
    int MaxIcon,
    string BadgePartsFile,
    IReadOnlyList<string> Pages,
    bool Force)
{
    public string Site => $"https://www.habbo.{Hotel}";
}
