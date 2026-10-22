namespace Habbo.Camera.Avatars;

/// <summary>The image an avatar of one size and geometry type is drawn into (the geometry's canvas): its size, the offset
/// its parts are moved by, and where the figure is registered, centred on its 64 (or 32) pixel wide body.</summary>
public sealed record AvatarCanvas(string Scale, int Width, int Height, int OffsetX, int OffsetY)
{
    public double RegX => Scale == AvatarImager.LargeScale ? (Width - 64) / 2d : (Width - 32) / 2d;
}
