namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>A snowball machine: makes snowballs now and then, up to what it holds, for players to take.</summary>
internal sealed class SnowballMachineGameObject : SnowballGivingGameObject
{
    public const int TypeId = 4;
    private const int BoundingRadiusValue = 1200;

    private readonly Direction8 _direction;

    public SnowballMachineGameObject(int id, int snowballs, int maxSnowballs, Tile tile, int fuseObjectId, Direction8 direction)
        : base(id, snowballs, maxSnowballs, tile, fuseObjectId)
    {
        _direction = direction;
        tile.AddGameObject(this);
    }

    public override int Type => TypeId;
    public override int VariableCount => 8;
    public override int BoundingRadius => BoundingRadiusValue;

    public override int GetVariable(int index) => index switch
    {
        0 => TypeId,
        1 => Id,
        2 => Tile.Location.X,
        3 => Tile.Location.Y,
        4 => _direction.Value,
        5 => MaxSnowballs,
        6 => SnowballCount,
        7 => FuseObjectId,
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    public void CreateSnowball()
    {
        if (SnowballCount < MaxSnowballs)
            SnowballCount++;
    }
}
