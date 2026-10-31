using System.Globalization;
using SkiaSharp;

namespace Habbo.Bundles;

/// <summary>The values of a manifest asset's params: <c>offset="x,y"</c>, <c>region="x,y,w,h"</c>.</summary>
internal static class ManifestParams
{
    public static bool TryParsePoint(string value, out (int X, int Y) point)
    {
        point = default;
        var parts = Numbers(value);
        if (parts is not { Length: 2 })
            return false;
        point = (parts[0], parts[1]);
        return true;
    }

    public static bool TryParseRegion(string value, out SKRectI region)
    {
        region = default;
        var parts = Numbers(value);
        if (parts is not { Length: 4 })
            return false;
        region = new SKRectI(parts[0], parts[1], parts[0] + parts[2], parts[1] + parts[3]);
        return true;
    }

    private static int[]? Numbers(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        var numbers = new int[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                return null;
            numbers[i] = (int)Math.Round(number);
        }
        return numbers;
    }
}
