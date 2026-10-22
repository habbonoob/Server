namespace Habbo.Camera;

/// <summary>What an asset is in its library: an alias of <see cref="Source"/> (flipped or not), or its own pixels —
/// a whole image entry, or a frame of a sprite sheet — with its offset from the object's origin.</summary>
internal sealed record AssetDefinition(string? Source, bool FlipH, bool FlipV, int X, int Y, string? Image, SheetFrame? Frame);
