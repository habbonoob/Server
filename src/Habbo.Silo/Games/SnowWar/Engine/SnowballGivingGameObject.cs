namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>Something players take snowballs from: a pile or a machine, standing on a tile, shown by a furni.</summary>
internal abstract class SnowballGivingGameObject : SnowWarGameObject
{
    protected SnowballGivingGameObject(int id, int snowballs, int maxSnowballs, Tile tile, int fuseObjectId) : base(id)
    {
        IsActive = true;
        SnowballCount = snowballs;
        MaxSnowballs = maxSnowballs;
        Tile = tile;
        FuseObjectId = fuseObjectId;
    }

    public int SnowballCount { get; protected set; }
    public int MaxSnowballs { get; }
    public Tile Tile { get; }
    public int FuseObjectId { get; }

    public override Location3D Location => Tile.Location;

    /// <summary>Takes up to so many snowballs; how many there were to take.</summary>
    public int PickupSnowballs(int count)
    {
        count = Math.Min(count, SnowballCount);
        SnowballCount -= count;
        OnSnowballPickup();
        return count;
    }

    protected virtual void OnSnowballPickup()
    {
    }
}
