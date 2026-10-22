namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>One tile of the arena: its neighbours, the furni on it, how high it is built up, and the one game object
/// that holds it (a player, a tree, a pile, a machine).</summary>
internal sealed class Tile(int x, int y)
{
    public const int Width = 3200;
    public const int HalfWidth = Width / 2;

    private readonly Tile?[] _neighbours = new Tile?[8];
    private readonly List<SnowWarFuseObject> _fuseObjects = [];

    public int X { get; } = x;
    public int Y { get; } = y;
    public Location3D Location { get; } = new(x * Width, y * Width, 0);
    public int Height { get; private set; }
    public bool Blocked { get; set; }
    public SnowWarGameObject? GameObject { get; private set; }

    public HumanGameObject? OccupyingHuman => GameObject as HumanGameObject;

    public static int ToTileX(int x) => (x + HalfWidth) / Width;
    public static int ToTileY(int y) => (y + HalfWidth) / Width;

    public void AddFuseObject(SnowWarFuseObject fuse)
    {
        _fuseObjects.Add(fuse);
        AddToHeight(fuse.Height);
    }

    public void AddToHeight(int height) => Height = Math.Max(0, Height + height);

    public bool LocationIsInTileRange(Location3D location) =>
        SnowWarMath.Abs(Location.X - location.X) < HalfWidth && SnowWarMath.Abs(Location.Y - location.Y) < HalfWidth;

    public void Link(Tile other, Direction8 direction)
    {
        _neighbours[direction.Value] = other;
        other._neighbours[direction.Opposite().Value] = this;
    }

    public Tile? InDirection(Direction8 direction) => _neighbours[direction.Value];

    /// <summary>Free to move onto: no furni in the way (one furni one may stand on is fine), nothing on it, not blocked.</summary>
    public bool CanMoveTo()
    {
        var furniInWay = _fuseObjects.Count > 1 || (_fuseObjects.Count == 1 && !_fuseObjects[0].CanStandOn);
        return !furniInWay && GameObject is null && !Blocked;
    }

    public bool AddGameObject(SnowWarGameObject gameObject)
    {
        if (GameObject is not null)
            return false;
        GameObject = gameObject;
        return true;
    }

    public void RemoveGameObject() => GameObject = null;

    public void RemoveOccupyingHuman()
    {
        if (OccupyingHuman is not null)
            GameObject = null;
    }
}
