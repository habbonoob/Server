namespace Habbo.Camera;

/// <summary>A rendered photo as PNGs: the photo, and the thumbnail a photo furni shows on the wall.</summary>
public sealed record RenderedPhoto(byte[] Png, byte[] SmallPng);
