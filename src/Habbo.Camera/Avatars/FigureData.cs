using System.Globalization;
using System.Xml.Linq;

namespace Habbo.Camera.Avatars;

/// <summary>figuredata.xml as the avatar renderer reads it: the palettes' colours, and every set type's sets.</summary>
public sealed class FigureData
{
    private readonly Dictionary<int, Dictionary<int, uint>> _palettes;
    private readonly Dictionary<string, FigureSetType> _setTypes;

    private FigureData(Dictionary<int, Dictionary<int, uint>> palettes, Dictionary<string, FigureSetType> setTypes)
    {
        _palettes = palettes;
        _setTypes = setTypes;
    }

    public FigureSetType? SetType(string type) => _setTypes.GetValueOrDefault(type);

    public bool HasPalette(int paletteId) => _palettes.ContainsKey(paletteId);

    /// <summary>A palette colour's RGB; null when the palette has no such colour.</summary>
    public uint? Color(int paletteId, int colorId) =>
        _palettes.TryGetValue(paletteId, out var palette) && palette.TryGetValue(colorId, out var rgb) ? rgb : null;

    public static FigureData Load(string path)
    {
        var root = XDocument.Load(path).Root ?? throw new InvalidDataException($"{path} is empty.");
        var palettes = new Dictionary<int, Dictionary<int, uint>>();
        foreach (var palette in root.Element("colors")?.Elements("palette") ?? [])
        {
            var colors = new Dictionary<int, uint>();
            foreach (var color in palette.Elements("color"))
                colors[Int(color, "id")] = uint.TryParse(color.Value.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb) ? rgb : 0;
            palettes[Int(palette, "id")] = colors;
        }

        var setTypes = new Dictionary<string, FigureSetType>(StringComparer.Ordinal);
        foreach (var setType in root.Element("sets")?.Elements("settype") ?? [])
        {
            var type = (string?)setType.Attribute("type") ?? string.Empty;
            var sets = new Dictionary<int, FigurePartSet>();
            foreach (var set in setType.Elements("set"))
            {
                var parts = set.Elements("part").Select(p => new FigurePart(
                    (string?)p.Attribute("type") ?? string.Empty, Int(p, "id"), Int(p, "index"), Int(p, "colorindex")));
                var hidden = set.Element("hiddenlayers")?.Elements("layer").Select(l => (string?)l.Attribute("parttype") ?? string.Empty) ?? [];
                sets[Int(set, "id")] = new FigurePartSet(parts, hidden);
            }
            setTypes[type] = new FigureSetType(type, Int(setType, "paletteid"), sets);
        }
        return new FigureData(palettes, setTypes);
    }

    private static int Int(XElement element, string attribute) =>
        int.TryParse((string?)element.Attribute(attribute), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
}
