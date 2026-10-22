using SkiaSharp;

namespace Habbo.Gateway.Tests.Camera;

/// <summary>Small PNGs, drawn pixel by pixel.</summary>
internal static class TestImages
{
    public static byte[] Png(int width, int height, Func<int, int, SKColor> pixel)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            bitmap.SetPixel(x, y, pixel(x, y));
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public static SKBitmap Decode(byte[] png) => SKBitmap.Decode(png);
}
