using System.Text.Json.Nodes;
using SkiaSharp;

namespace Habbo.Bundles;

/// <summary>A frame of a TexturePacker sheet: where its pixels are, and, for a trimmed one, where they sit in the image
/// it was trimmed from. Habbo draws the image whole, its offset from that image's top left, so the trimmed margins
/// are put back.</summary>
internal readonly record struct SheetFrame(SKRectI Rect, int TrimX, int TrimY, int SourceWidth, int SourceHeight)
{
    public static SheetFrame Read(JsonNode frame)
    {
        var rect = frame["frame"]!;
        var (x, y) = (rect["x"]!.GetValue<int>(), rect["y"]!.GetValue<int>());
        var (width, height) = (rect["w"]!.GetValue<int>(), rect["h"]!.GetValue<int>());
        var trimmed = frame["trimmed"]?.GetValue<bool>() == true;
        var spriteSource = frame["spriteSourceSize"];
        var sourceSize = frame["sourceSize"];
        if (!trimmed || spriteSource is null || sourceSize is null)
            return new SheetFrame(new SKRectI(x, y, x + width, y + height), 0, 0, width, height);
        return new SheetFrame(
            new SKRectI(x, y, x + width, y + height),
            spriteSource["x"]!.GetValue<int>(),
            spriteSource["y"]!.GetValue<int>(),
            sourceSize["w"]!.GetValue<int>(),
            sourceSize["h"]!.GetValue<int>());
    }

    /// <summary>The image as it was before trimming: its pixels at their place, the margins transparent.</summary>
    public SKBitmap Cut(SKBitmap sheet)
    {
        if (TrimX == 0 && TrimY == 0 && SourceWidth == Rect.Width && SourceHeight == Rect.Height)
            return Bitmaps.Crop(sheet, Rect);
        var info = new SKImageInfo(Math.Max(1, SourceWidth), Math.Max(1, SourceHeight), Bitmaps.ColorType, Bitmaps.AlphaType);
        var bitmap = new SKBitmap(info);
        bitmap.Erase(SKColors.Transparent);
        Bitmaps.Blit(sheet, Rect, bitmap, TrimX, TrimY);
        return bitmap;
    }
}
