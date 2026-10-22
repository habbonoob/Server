using System.Globalization;
using System.Xml.Linq;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Users;

/// <summary>Checks a look against Habbo's figure data, the same file the client draws avatars
/// from: every part must be a set of its type the player's gender and membership may wear, in
/// colours of that type's palette they may use, and every type the data makes mandatory must be
/// there. A set the data marks sellable is worn only once a clothing furni has unlocked it.</summary>
public sealed class FigureValidator(IDbContextFactory<AppDbContext> dbFactory)
{
    /// <summary>The figure data's name in web_gamedata.</summary>
    private const string GamedataName = "figuredata";

    private const char PartSeparator = '.';
    private const char FieldSeparator = '-';
    private const string UnisexGender = "U";
    private const string Enabled = "1";

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private FigureData? _data;

    /// <summary>Whether <paramref name="figure"/> is a look this player may wear.</summary>
    public async Task<bool> IsValidAsync(
        string figure, Gender gender, ClubLevel club, IReadOnlySet<int> unlockedSets, CancellationToken ct = default)
    {
        var data = await LoadAsync(ct);
        if (data.SetTypes.Count == 0 || string.IsNullOrWhiteSpace(figure))
            return false;

        var genderLetter = gender.ToLetter();
        var worn = new HashSet<string>(StringComparer.Ordinal);

        foreach (var part in figure.Split(PartSeparator))
        {
            var fields = part.Split(FieldSeparator);
            if (fields.Length < 2 || !worn.Add(fields[0]))
                return false;

            if (!data.SetTypes.TryGetValue(fields[0], out var setType)
                || !int.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out var setId)
                || !setType.Sets.TryGetValue(setId, out var set))
                return false;

            if (set.Gender != UnisexGender && set.Gender != genderLetter)
                return false;
            if (set.Club > (int)club)
                return false;
            if (set.Sellable ? !unlockedSets.Contains(set.Id) : !set.Selectable)
                return false;

            if (!data.Palettes.TryGetValue(setType.PaletteId, out var palette))
                return false;

            foreach (var colourField in fields.Skip(2))
            {
                if (!int.TryParse(colourField, NumberStyles.None, CultureInfo.InvariantCulture, out var colourId)
                    || !palette.TryGetValue(colourId, out var colour)
                    || !colour.Selectable
                    || colour.Club > (int)club)
                    return false;
            }
        }

        var clubKey = club > ClubLevel.None ? 1 : 0;
        return data.SetTypes.Values
            .Where(setType => setType.IsMandatory(genderLetter, clubKey))
            .All(setType => worn.Contains(setType.Type));
    }

    /// <summary>The sets a clothing furni's custom params name, which exist in the figure data.</summary>
    public async Task<IReadOnlyList<int>> KnownSetsAsync(IEnumerable<int> setIds, CancellationToken ct = default)
    {
        var data = await LoadAsync(ct);
        var known = data.SetTypes.Values.SelectMany(setType => setType.Sets.Keys).ToHashSet();
        return setIds.Where(known.Contains).Distinct().ToList();
    }

    private async Task<FigureData> LoadAsync(CancellationToken ct)
    {
        if (_data is not null)
            return _data;

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_data is not null)
                return _data;

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var content = await db.WebGamedata.AsNoTracking()
                .Where(g => g.Name == GamedataName)
                .Select(g => g.Content)
                .FirstOrDefaultAsync(ct);

            _data = content is null ? FigureData.Empty : FigureData.Parse(content);
            return _data;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private sealed record FigureData(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, FigureColour>> Palettes,
        IReadOnlyDictionary<string, FigureSetType> SetTypes)
    {
        public static FigureData Empty { get; } = new(
            new Dictionary<int, IReadOnlyDictionary<int, FigureColour>>(),
            new Dictionary<string, FigureSetType>());

        public static FigureData Parse(string xml)
        {
            var root = XDocument.Parse(xml).Root!;
            var palettes = root.Element("colors")?.Elements("palette")
                .ToDictionary(
                    palette => Int(palette, "id"),
                    palette => (IReadOnlyDictionary<int, FigureColour>)palette.Elements("color")
                        .ToDictionary(
                            colour => Int(colour, "id"),
                            colour => new FigureColour(Int(colour, "club"), (string?)colour.Attribute("selectable") == Enabled)))
                ?? [];

            var setTypes = root.Element("sets")?.Elements("settype")
                .ToDictionary(
                    setType => (string)setType.Attribute("type")!,
                    setType => new FigureSetType(
                        (string)setType.Attribute("type")!,
                        Int(setType, "paletteid"),
                        setType.Attributes().Where(a => a.Name.LocalName.StartsWith("mand_", StringComparison.Ordinal))
                            .ToDictionary(a => a.Name.LocalName, a => a.Value == Enabled),
                        setType.Elements("set").ToDictionary(
                            set => Int(set, "id"),
                            set => new FigureSet(
                                Int(set, "id"),
                                (string?)set.Attribute("gender") ?? UnisexGender,
                                Int(set, "club"),
                                (string?)set.Attribute("selectable") == Enabled,
                                (string?)set.Attribute("sellable") == Enabled))),
                    StringComparer.Ordinal)
                ?? [];

            return new FigureData(palettes, setTypes);
        }

        private static int Int(XElement element, string attribute) =>
            int.TryParse((string?)element.Attribute(attribute), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
    }

    private sealed record FigureColour(int Club, bool Selectable);

    private sealed record FigureSet(int Id, string Gender, int Club, bool Selectable, bool Sellable);

    /// <summary>A set type (hair, shirt…). Mandatory flags are keyed "mand_{m|f}_{0|1}", the digit
    /// saying whether the rule is the club member's.</summary>
    private sealed record FigureSetType(
        string Type, int PaletteId, IReadOnlyDictionary<string, bool> Mandatory, IReadOnlyDictionary<int, FigureSet> Sets)
    {
        public bool IsMandatory(string genderLetter, int clubKey) =>
            Mandatory.GetValueOrDefault($"mand_{genderLetter.ToLowerInvariant()}_{clubKey}");
    }
}
