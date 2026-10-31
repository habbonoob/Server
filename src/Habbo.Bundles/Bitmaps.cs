using SkiaSharp;

namespace Habbo.Bundles;

/// <summary>Decoding and cutting images with their exact pixels: unpremultiplied RGBA throughout, so what Habbo drew
/// is what the atlas holds.</summary>
internal static class Bitmaps
{
    public const SKColorType ColorType = SKColorType.Rgba8888;
    public const SKAlphaType AlphaType = SKAlphaType.Unpremul;

    public static SKBitmap Decode(byte[] data, string name)
    {
        using var codec = SKCodec.Create(new MemoryStream(data))
            ?? throw new InvalidDataException($"{name} is not an image Skia can read.");
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, ColorType, AlphaType);
        var bitmap = new SKBitmap(info);
        var result = codec.GetPixels(info, bitmap.GetPixels());
        if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
        {
            bitmap.Dispose();
            throw new InvalidDataException($"{name} could not be decoded: {result}.");
        }
        return bitmap;
    }

    /// <summary>A copy of a rectangle of the bitmap; the part outside it is left transparent.</summary>
    public static SKBitmap Crop(SKBitmap source, SKRectI rect)
    {
        var bitmap = new SKBitmap(new SKImageInfo(Math.Max(1, rect.Width), Math.Max(1, rect.Height), ColorType, AlphaType));
        bitmap.Erase(SKColors.Transparent);
        Blit(source, rect, bitmap, 0, 0);
        return bitmap;
    }

    /// <summary>Copies a rectangle of one bitmap into another at (x, y), byte for byte: no blending.</summary>
    public static void Blit(SKBitmap source, SKRectI rect, SKBitmap target, int x, int y)
    {
        var from = SKRectI.Intersect(rect, new SKRectI(0, 0, source.Width, source.Height));
        if (from.IsEmpty)
            return;
        var dx = x + (from.Left - rect.Left);
        var dy = y + (from.Top - rect.Top);
        var width = Math.Min(from.Width, target.Width - dx);
        if (width <= 0)
            return;
        var sourceBytes = source.GetPixelSpan();
        var targetBytes = target.GetPixelSpan();
        for (var row = 0; row < from.Height && dy + row < target.Height; row++)
        {
            sourceBytes.Slice((from.Top + row) * source.RowBytes + from.Left * 4, width * 4)
                .CopyTo(targetBytes.Slice((dy + row) * target.RowBytes + dx * 4, width * 4));
        }
    }
}
