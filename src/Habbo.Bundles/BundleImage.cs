using SkiaSharp;

namespace Habbo.Bundles;

/// <summary>A named image of a library. Either it has pixels of its own, or it is drawn from another image
/// (<see cref="Source"/>), maybe flipped. The offset is as the source gives it, in Habbo's own sign.</summary>
public sealed record BundleImage(
    string Name,
    SKBitmap? Pixels,
    string? Source,
    int OffsetX,
    int OffsetY,
    bool HasOffset,
    bool FlipH,
    bool FlipV);
