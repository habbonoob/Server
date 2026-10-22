namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>A pile of snowballs: players take from it until it is gone, and it shrinks as they do.</summary>
internal sealed class SnowballPileGameObject : SnowballGivingGameObject
{
    public const int TypeId = 3;
    private const int BoundingRadiusPerSnowball = 100;

    public SnowballPileGameObject(int id, int snowballs, int maxSnowballs, Tile tile, int fuseObjectId)
        : base(id, snowballs, maxSnowballs, tile, fuseObjectId)
    {
        if (SnowballCount > 0)
            tile.AddGameObject(this);
        BoundingRadiusNow = SnowballCount * BoundingRadiusPerSnowball;
    }

    private int BoundingRadiusNow { get; set; }

    public override int Type => TypeId;
    public override int VariableCount => 7;
    public override int BoundingRadius => BoundingRadiusNow;

    public override int GetVariable(int index) => index switch
    {
        0 => TypeId,
        1 => Id,
        2 => Tile.Location.X,
        3 => Tile.Location.Y,
        4 => MaxSnowballs,
        5 => SnowballCount,
        6 => FuseObjectId,
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    protected override void OnSnowballPickup()
    {
        BoundingRadiusNow = SnowballCount * BoundingRadiusPerSnowball;
        if (SnowballCount <= 0)
            Tile.RemoveGameObject();
    }
}
