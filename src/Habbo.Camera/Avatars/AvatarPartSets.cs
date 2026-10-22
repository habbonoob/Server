using System.Xml.Linq;

namespace Habbo.Camera.Avatars;

/// <summary>HabboAvatarPartSets, as PartSetsData reads it: the part type each one is drawn as when the avatar faces the
/// other way (a right hand as the left), and the part types each action's active part set moves.</summary>
public sealed class AvatarPartSets
{
    private readonly Dictionary<string, string> _flipped = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _active = new(StringComparer.Ordinal);

    private AvatarPartSets(XElement root)
    {
        foreach (var part in root.Element("partSet")?.Elements("part") ?? [])
            _flipped[(string?)part.Attribute("set-type") ?? string.Empty] = (string?)part.Attribute("flipped-set-type") ?? string.Empty;
        foreach (var set in root.Elements("activePartSet"))
            _active[(string?)set.Attribute("id") ?? string.Empty] =
                set.Elements("activePart").Select(p => (string?)p.Attribute("set-type") ?? string.Empty).ToList();
    }

    public static AvatarPartSets Load(string path) =>
        new(XDocument.Load(path).Root ?? throw new InvalidDataException($"{path} is empty."));

    /// <summary>The type a part is drawn as when mirrored: its flipped set type, or itself when it has none.</summary>
    public string FlippedType(string type) =>
        _flipped.TryGetValue(type, out var flipped) && flipped.Length > 0 ? flipped : type;

    public IReadOnlyList<string> ActiveParts(string activePartSet) => _active.GetValueOrDefault(activePartSet) ?? [];
}
