using System.Globalization;
using Habbo.Silo.Abstractions.Hotel;

namespace Habbo.Silo.Rooms;

/// <summary>The colours the recolour window offers a recolourable furni, read from the client variables as the client
/// reads them: recolorable_bc.palette_to_furni names each palette's furni ("name=class,class;name=..."), a furni no
/// palette names takes "default", and recolorable_bc.palette.&lt;name&gt; lists the palette's colours in hex.</summary>
public static class RecolorPalettes
{
    public const string PaletteToFurniKey = "recolorable_bc.palette_to_furni";
    public const string PaletteKeyPrefix = "recolorable_bc.palette.";
    public const string DefaultPalette = "default";

    /// <summary>The colours (0xRRGGBB) the furni of this class may be given; none when its palette is not set.</summary>
    public static async Task<IReadOnlySet<int>> ForAsync(IClientVariables variables, string className)
    {
        var palette = DefaultPalette;
        // the last palette naming the class wins, as in the client
        foreach (var entry in (await variables.GetAsync(PaletteToFurniKey) ?? string.Empty).Split(';'))
        {
            var parts = entry.Split('=');
            if (parts.Length == 2 && parts[1].Split(',').Any(name => name.Trim() == className))
                palette = parts[0].Trim();
        }

        return (await variables.GetAsync(PaletteKeyPrefix + palette) ?? string.Empty).Split(',')
            .Select(hex => hex.Trim())
            .Where(hex => hex.Length == 6 && hex.All(char.IsAsciiHexDigit))
            .Select(hex => int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            .ToHashSet();
    }
}
