namespace Habbo.Tools.AssetMirror;

/// <summary>/gamedata/hashes: the gamedata files, each at its url followed by its hash.</summary>
public sealed record GamedataManifest(IReadOnlyList<GamedataHash> Hashes);
