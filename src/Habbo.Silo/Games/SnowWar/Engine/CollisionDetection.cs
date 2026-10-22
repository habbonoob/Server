namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>Whether a snowball touches an object. Everything in the game is a circle (the client's bounding type 2),
/// so only the circle-to-circle test is needed.</summary>
internal static class CollisionDetection
{
    public const int CircleBounding = 2;

    public static bool Collides(SnowWarGameObject first, SnowWarGameObject second)
    {
        if (ReferenceEquals(first, second) || second.BoundingType != CircleBounding || first.BoundingType != CircleBounding)
            return false;
        return first.Location!.IsInDistance(second.Location!, first.BoundingRadius + second.BoundingRadius);
    }
}
