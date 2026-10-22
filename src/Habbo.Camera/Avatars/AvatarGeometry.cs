using System.Globalization;
using System.Xml.Linq;

namespace Habbo.Camera.Avatars;

/// <summary>HabboAvatarGeometry, as AvatarModelGeometry reads it: the canvases, the avatar sets, and each geometry type's
/// body parts with their items, from which the draw order at any direction is worked out.</summary>
public sealed class AvatarGeometry
{
    private readonly double _cameraZ;
    private readonly Dictionary<string, Dictionary<string, AvatarCanvas>> _canvases = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<AvatarGeometryBodyPart>> _types = new(StringComparer.Ordinal);
    private readonly List<(string BodyPart, string Beside)> _orderBefore = [];
    private readonly List<(string BodyPart, string Beside)> _orderAfter = [];
    private readonly AvatarSet _avatarSet;

    private AvatarGeometry(XElement root)
    {
        _cameraZ = Number(root.Element("camera")?.Element("z")?.Value, 10);
        foreach (var canvas in root.Elements("canvas"))
        {
            var scale = (string?)canvas.Attribute("scale") ?? string.Empty;
            _canvases[scale] = canvas.Elements("geometry").ToDictionary(
                g => (string?)g.Attribute("id") ?? string.Empty,
                g => new AvatarCanvas(scale, Int(g, "width"), Int(g, "height"), Int(g, "dx"), Int(g, "dy")), StringComparer.Ordinal);
        }
        _avatarSet = AvatarSet.Read(root.Element("avatarset") ?? throw new InvalidDataException("The geometry has no avatarset."));
        foreach (var type in root.Elements("type"))
        {
            var bodyParts = new List<AvatarGeometryBodyPart>();
            foreach (var bodyPart in type.Elements("bodypart"))
            {
                var id = (string?)bodyPart.Attribute("id") ?? string.Empty;
                bodyParts.Add(new AvatarGeometryBodyPart(Node(bodyPart), bodyPart.Descendants("item").Select(Node).ToList()));
                if ((string?)bodyPart.Attribute("order-before") is { Length: > 0 } before)
                    Remember(_orderBefore, id, before);
                if ((string?)bodyPart.Attribute("order-after") is { Length: > 0 } after)
                    Remember(_orderAfter, id, after);
            }
            _types[(string?)type.Attribute("id") ?? string.Empty] = bodyParts;
        }
    }

    public static AvatarGeometry Load(string path) =>
        new(XDocument.Load(path).Root ?? throw new InvalidDataException($"{path} is empty."));

    public AvatarCanvas? Canvas(string scale, string geometryType) =>
        _canvases.TryGetValue(scale, out var canvases) ? canvases.GetValueOrDefault(geometryType) : null;

    /// <summary>The body parts of an avatar set ("full", "head").</summary>
    public IReadOnlyList<string> BodyPartsInSet(string setId) => _avatarSet.Find(setId)?.BodyParts ?? [];

    /// <summary>The set's body parts nearest the camera first, turned to the angle (getBodyPartsAtAngle), then moved before
    /// or after the ones the geometry says they go by.</summary>
    public List<string> BodyPartsAtAngle(string setId, double angle, string geometryType)
    {
        if (!_types.TryGetValue(geometryType, out var bodyParts))
            return [];
        var inSet = BodyPartsInSet(setId);
        var ordered = inSet.Select(id => bodyParts.FirstOrDefault(b => b.Id == id)).OfType<AvatarGeometryBodyPart>()
            .OrderBy(b => b.Node.Distance(angle, _cameraZ)).Select(b => b.Id).ToList();
        foreach (var (id, before) in _orderBefore)
            Move(ordered, id, before, after: false);
        foreach (var (id, after) in _orderAfter)
            Move(ordered, id, after, after: true);
        return ordered;
    }

    /// <summary>A body part's part types nearest the camera first. The client turns them by the direction's number as
    /// degrees, not by its angle (AvatarModelGeometry.getParts is handed the direction), and so does this.</summary>
    public List<string> PartsOf(string geometryType, string bodyPartId, int direction)
    {
        if (!_types.TryGetValue(geometryType, out var bodyParts) || bodyParts.FirstOrDefault(b => b.Id == bodyPartId) is not { } bodyPart)
            return [];
        return bodyPart.Items.OrderBy(item => item.Distance(direction, _cameraZ)).Select(item => item.Id).ToList();
    }

    /// <summary>As the client's dictionary keeps it: a body part named again keeps its place and takes the new value.</summary>
    private static void Remember(List<(string BodyPart, string Beside)> rules, string id, string beside)
    {
        var at = rules.FindIndex(r => r.BodyPart == id);
        if (at == -1)
            rules.Add((id, beside));
        else
            rules[at] = (id, beside);
    }

    private static void Move(List<string> order, string id, string beside, bool after)
    {
        var at = order.IndexOf(id);
        if (at == -1)
            return;
        order.RemoveAt(at);
        var target = order.IndexOf(beside);
        if (target == -1)
            return;
        order.Insert(after ? target + 1 : target, id);
    }

    private static AvatarGeometryNode Node(XElement element) => new(
        (string?)element.Attribute("id") ?? string.Empty,
        Number((string?)element.Attribute("x"), 0), Number((string?)element.Attribute("y"), 0), Number((string?)element.Attribute("z"), 0),
        Number((string?)element.Attribute("radius"), 0));

    private static double Number(string? text, double fallback) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    private static int Int(XElement element, string attribute) =>
        int.TryParse((string?)element.Attribute(attribute), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
}
