namespace Habbo.Bundles;

/// <summary>Where each image went, in the order given, and how big each page came out.</summary>
public sealed record AtlasLayout(IReadOnlyList<AtlasPlacement> Placements, IReadOnlyList<(int Width, int Height)> Pages);
