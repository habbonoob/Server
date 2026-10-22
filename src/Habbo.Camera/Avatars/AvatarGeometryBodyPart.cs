namespace Habbo.Camera.Avatars;

/// <summary>A body part of a geometry type ("head", "torso"…): where it sits, and the part types drawn within it.</summary>
public sealed record AvatarGeometryBodyPart(AvatarGeometryNode Node, IReadOnlyList<AvatarGeometryNode> Items)
{
    public string Id => Node.Id;
}
