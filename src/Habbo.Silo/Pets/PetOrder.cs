using System.Text.RegularExpressions;

namespace Habbo.Silo.Pets;

/// <summary>What the shop's pet page sends with a pet purchase: "name\npalette\ncolour", the colour as six
/// hex digits.</summary>
public sealed partial record PetOrder(string Name, int PaletteId, string Color)
{
    private const char Separator = '\n';

    [GeneratedRegex("^[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();

    public static PetOrder? Parse(string extraData)
    {
        var parts = extraData.Split(Separator);
        return parts.Length == 3 && int.TryParse(parts[1], out var palette) && HexColor().IsMatch(parts[2])
            ? new PetOrder(parts[0], palette, parts[2].ToUpperInvariant())
            : null;
    }
}
