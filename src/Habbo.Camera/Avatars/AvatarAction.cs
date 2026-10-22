using System.Xml.Linq;

namespace Habbo.Camera.Avatars;

/// <summary>An avatar action (HabboAvatarActions' action): the posture its assets are drawn in ("std"), the part types it
/// moves, and the geometry it is laid out on.</summary>
public sealed record AvatarAction(string Id, string AssetPartDefinition, string ActivePartSet, string GeometryType)
{
    /// <summary>The action an avatar is in when nothing else is going on: the one marked isdefault.</summary>
    public static AvatarAction LoadDefault(string path)
    {
        var root = XDocument.Load(path).Root ?? throw new InvalidDataException($"{path} is empty.");
        var action = root.Elements("action").FirstOrDefault(a => (string?)a.Attribute("isdefault") == "1")
                     ?? throw new InvalidDataException($"{path} has no default action.");
        return new AvatarAction(
            (string?)action.Attribute("id") ?? string.Empty,
            (string?)action.Attribute("assetpartdefinition") ?? string.Empty,
            (string?)action.Attribute("activepartset") ?? string.Empty,
            (string?)action.Attribute("geometrytype") ?? string.Empty);
    }
}
