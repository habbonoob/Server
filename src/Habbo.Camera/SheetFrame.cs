using System.Text.Json;

namespace Habbo.Camera;

/// <summary>Where a sprite is in a sprite sheet, and where in its untrimmed size it goes.</summary>
internal sealed record SheetFrame(int X, int Y, int Width, int Height, int TrimX, int TrimY, int SourceWidth, int SourceHeight)
{
    public static SheetFrame From(JsonElement json)
    {
        var frame = json.GetProperty("frame");
        var w = Get(frame, "w", 0);
        var h = Get(frame, "h", 0);
        var trim = json.TryGetProperty("spriteSourceSize", out var t) ? t : default;
        var size = json.TryGetProperty("sourceSize", out var s) ? s : default;
        return new SheetFrame(Get(frame, "x", 0), Get(frame, "y", 0), w, h, Get(trim, "x", 0), Get(trim, "y", 0), Get(size, "w", w), Get(size, "h", h));
    }

    private static int Get(JsonElement e, string key, int fallback) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? (int)v.GetDouble() : fallback;
}
