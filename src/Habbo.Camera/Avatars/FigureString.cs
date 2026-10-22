using System.Globalization;

namespace Habbo.Camera.Avatars;

/// <summary>A figure string as the client reads it (AvatarFigureContainer): "hr-115-42.hd-190-1", a set of each type with
/// its colours. A type named twice keeps its first place and its last set.</summary>
public sealed class FigureString
{
    private readonly List<string> _types = [];
    private readonly Dictionary<string, (int SetId, IReadOnlyList<int> Colors)> _parts = new(StringComparer.Ordinal);

    private FigureString()
    {
    }

    /// <summary>The set types, in the order the figure first names them.</summary>
    public IReadOnlyList<string> Types => _types;

    public int SetId(string type) => _parts[type].SetId;

    public IReadOnlyList<int> Colors(string type) => _parts[type].Colors;

    public static FigureString Parse(string figure)
    {
        var parsed = new FigureString();
        foreach (var piece in figure.Split('.'))
        {
            var fields = piece.Split('-');
            if (fields.Length < 2)
                continue;
            var type = fields[0];
            var setId = ParseInt(fields[1]);
            var colors = fields.Skip(2).Select(ParseInt).ToList();
            if (!parsed._parts.ContainsKey(type))
                parsed._types.Add(type);
            parsed._parts[type] = (setId, colors);
        }
        return parsed;
    }

    /// <summary>AS3's parseInt: the leading digits, 0 for none.</summary>
    private static int ParseInt(string text)
    {
        var digits = text.TakeWhile((c, i) => char.IsAsciiDigit(c) || (i == 0 && c == '-')).ToArray();
        return int.TryParse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }
}
