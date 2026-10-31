using SkiaSharp;

namespace Habbo.Bundles.Tests;

/// <summary>Small images drawn pixel by pixel, and their pixels read back exactly.</summary>
internal static class TestImages
{
    public static byte[] Png(int width, int height, Func<int, int, SKColor> pixel)
    {
        using var bitmap = Bitmap(width, height, pixel);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public static SKBitmap Bitmap(int width, int height, Func<int, int, SKColor> pixel)
    {
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            bitmap.SetPixel(x, y, pixel(x, y));
        return bitmap;
    }

    /// <summary>An image's pixels as unpremultiplied RGBA, so a half-transparent pixel compares exactly.</summary>
    public static SKBitmap DecodeExact(byte[] data)
    {
        using var codec = SKCodec.Create(new MemoryStream(data));
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var bitmap = new SKBitmap(info);
        codec.GetPixels(info, bitmap.GetPixels());
        return bitmap;
    }
}
