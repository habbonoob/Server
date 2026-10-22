namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>A tree: stops snowballs until it has taken all the hits it can, and nobody walks through it.</summary>
internal sealed class TreeGameObject : SnowWarGameObject
{
    public const int TypeId = 2;

    /// <summary>A standing tree fills its tile but for a snowball's width.</summary>
    private const int StandingRadius = Tile.Width - 400 - 1;

    private readonly Tile _tile;
    private readonly Direction8 _direction;

    public TreeGameObject(int id, Tile tile, Direction8 direction, int height, int fuseObjectId, int hits, int maxHits) : base(id)
    {
        IsActive = true;
        _tile = tile;
        _direction = direction;
        Direction = new Direction360(Direction360.FromDirection8(direction));
        FuseObjectId = fuseObjectId;
        TreeHeight = height;
        Hits = hits;
        MaxHits = maxHits;
        if (Hits < MaxHits)
            tile.AddGameObject(this);
        tile.AddToHeight(-height);
        tile.Blocked = true;
    }

    public Direction360 Direction { get; }
    public int FuseObjectId { get; }
    public int TreeHeight { get; }
    public int Hits { get; private set; }
    public int MaxHits { get; }

    public override int Type => TypeId;
    public override int VariableCount => 9;
    public override int BoundingRadius => Hits < MaxHits ? StandingRadius : 0;
    public override Location3D Location => _tile.Location;
    public override int CollisionHeight => TreeHeight;

    public override int GetVariable(int index) => index switch
    {
        0 => TypeId,
        1 => Id,
        2 => _tile.Location.X,
        3 => _tile.Location.Y,
        4 => _direction.Value,
        5 => TreeHeight,
        6 => FuseObjectId,
        7 => MaxHits,
        8 => Hits,
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    public override void OnSnowballHit(SnowWarStage stage, SnowballGameObject snowball)
    {
        if (Hits < MaxHits)
            Hits++;
        if (Hits >= MaxHits)
            _tile.RemoveGameObject();
    }
}
