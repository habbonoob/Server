using SkiaSharp;

namespace Habbo.Camera.Avatars;

/// <summary>A body part drawn (AvatarImageBodyPartContainer): its image, and where it goes on the avatar's canvas.</summary>
public sealed record AvatarBodyPartImage(SKBitmap Bitmap, int X, int Y) : IDisposable
{
    public void Dispose() => Bitmap.Dispose();
}
