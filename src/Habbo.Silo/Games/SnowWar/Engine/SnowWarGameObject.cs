namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>Something in the game with a place in the checksum: its variables, in the client's order, are what the
/// checksum adds up and what goes on the wire.</summary>
internal abstract class SnowWarGameObject(int id)
{
    public int Id { get; } = id;

    public bool IsActive { get; set; }

    /// <summary>The client's number for the kind, the first variable.</summary>
    public abstract int Type { get; }

    public abstract int VariableCount { get; }

    public abstract int GetVariable(int index);

    public virtual int BoundingType => CollisionDetection.CircleBounding;

    public virtual int BoundingRadius => 0;

    public virtual Location3D? Location => null;

    /// <summary>How high a snowball must fly to pass over it.</summary>
    public virtual int CollisionHeight => BoundingRadius;

    public virtual void Subturn(SnowWarStage stage)
    {
    }

    public virtual void OnRemove()
    {
    }

    public virtual bool TestSnowballCollision(SnowballGameObject snowball) =>
        snowball.Location.Z < CollisionHeight && CollisionDetection.Collides(this, snowball);

    public virtual void OnSnowballHit(SnowWarStage stage, SnowballGameObject snowball)
    {
    }

    public IReadOnlyList<int> Variables() => Enumerable.Range(0, VariableCount).Select(GetVariable).ToList();
}
