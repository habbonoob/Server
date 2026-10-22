using System.Xml.Linq;

namespace Habbo.Camera.Avatars;

/// <summary>A named group of body parts ("full", "body", "head"), as the geometry's avatarset nests them.</summary>
public sealed class AvatarSet
{
    private readonly List<AvatarSet> _children;
    private readonly List<string> _bodyParts;

    private AvatarSet(XElement element)
    {
        Id = (string?)element.Attribute("id") ?? string.Empty;
        _children = element.Elements("avatarset").Select(e => new AvatarSet(e)).ToList();
        // its own body parts, then each child set's
        _bodyParts = element.Elements("bodypart").Select(b => (string?)b.Attribute("id") ?? string.Empty)
            .Concat(_children.SelectMany(c => c.BodyParts)).ToList();
    }

    public string Id { get; }

    public IReadOnlyList<string> BodyParts => _bodyParts;

    public static AvatarSet Read(XElement element) => new(element);

    /// <summary>The set by its id, as AvatarSet.findAvatarSet finds it: this one, or the child whose subtree holds it
    /// (the child itself, not the set found deeper down).</summary>
    public AvatarSet? Find(string id)
    {
        if (id == Id)
            return this;
        return _children.FirstOrDefault(child => child.Find(id) is not null);
    }
}
