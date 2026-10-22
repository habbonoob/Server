using SkiaSharp;

namespace Habbo.Camera.Avatars;

/// <summary>A part's bitmap ready to be put together with the rest of its body part (ImageData): where it registers,
/// whether it is mirrored on its own, and the colour it is tinted with.</summary>
public sealed record AvatarPartImage(SKBitmap Bitmap, int RegX, int RegY, bool FlipH, uint? Tint)
{
    /// <summary>The bitmap's rectangle placed by its registration point (offsetRect).</summary>
    public SKRectI OffsetRect => new(-RegX, -RegY, -RegX + Bitmap.Width, -RegY + Bitmap.Height);
}
