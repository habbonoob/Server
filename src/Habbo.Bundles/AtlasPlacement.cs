namespace Habbo.Bundles;

/// <summary>Where an image goes: its atlas page and position there.</summary>
public readonly record struct AtlasPlacement(int Page, int X, int Y);
