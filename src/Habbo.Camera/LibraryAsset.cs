using SkiaSharp;

namespace Habbo.Camera;

/// <summary>An asset's pixels and how its library has it drawn: mirrored or not (an alias of another asset, flipped),
/// and its offset from the object's origin.</summary>
public sealed record LibraryAsset(SKBitmap Bitmap, bool FlipH, bool FlipV, int OffsetX, int OffsetY);
