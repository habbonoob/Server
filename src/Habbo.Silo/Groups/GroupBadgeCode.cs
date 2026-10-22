using System.Globalization;
using System.Text;
using Habbo.Domain.Groups;

namespace Habbo.Silo.Groups;

/// <summary>Writes a badge's layers as the code the imager draws: "b" and the base, then "s" and
/// each symbol, each as part (two digits), colour (two digits) and position (one digit). A symbol
/// numbered 100 or more goes by "t" and its number less 100, so the part keeps two digits.</summary>
public static class GroupBadgeCode
{
    /// <summary>The badge editor's layers: the base and four symbols.</summary>
    public const int MaxLayers = 5;

    /// <summary>The nine positions on the badge's three-by-three grid.</summary>
    public const int Positions = 9;

    private const string BasePrefix = "b";
    private const string SymbolPrefix = "s";
    private const string HighSymbolPrefix = "t";
    private const int HighSymbolOffset = 100;
    private const string TwoDigits = "D2";

    public static string Build(IEnumerable<GroupBadgeLayer> layers)
    {
        var code = new StringBuilder();
        foreach (var layer in layers.OrderBy(l => l.Layer))
        {
            var partId = layer.PartId;
            var prefix = layer.Layer == 0 ? BasePrefix : SymbolPrefix;
            if (layer.Layer > 0 && partId >= HighSymbolOffset)
            {
                prefix = HighSymbolPrefix;
                partId -= HighSymbolOffset;
            }

            code.Append(prefix)
                .Append(partId.ToString(TwoDigits, CultureInfo.InvariantCulture))
                .Append(layer.ColorId.ToString(TwoDigits, CultureInfo.InvariantCulture))
                .Append(layer.Position.ToString(CultureInfo.InvariantCulture));
        }

        return code.ToString();
    }
}
