namespace Habbo.Silo.Games.SnowWar.Engine;

/// <summary>The arena's ground and everything on it: the tiles built from the height map and the furni, and the game
/// objects in the order they came, which is the order they move in each subturn. A port of the client's
/// SnowWarGameStage and SynchronizedGameStage.</summary>
internal sealed class SnowWarStage
{
    /// <summary>A height map square nobody may enter.</summary>
    private const char VoidTile = 'x';

    private readonly Tile?[][] _tiles;
    private readonly List<SnowWarGameObject> _objects = [];
    private readonly Dictionary<int, SnowWarGameObject> _objectsById = [];
    private readonly List<SnowWarGameObject> _toDelete = [];

    public SnowWarStage(SnowWarArena arena, int width, int height, string heightMap, IEnumerable<SnowWarFuseObject> fuseObjects)
    {
        Arena = arena;
        Width = width;
        Height = height;
        var rows = heightMap.Split('\r');
        _tiles = new Tile?[height][];
        for (var y = 0; y < height; y++)
        {
            _tiles[y] = new Tile?[width];
            var row = y < rows.Length ? rows[y] : string.Empty;
            for (var x = 0; x < width; x++)
            {
                if (x < row.Length && row[x] == VoidTile)
                    continue;

                var tile = new Tile(x, y);
                _tiles[y][x] = tile;
                LinkIfThere(tile, x + 1, y - 1, Direction8.NE);
                LinkIfThere(tile, x, y - 1, Direction8.N);
                LinkIfThere(tile, x - 1, y - 1, Direction8.NW);
                LinkIfThere(tile, x - 1, y, Direction8.W);
            }
        }

        foreach (var fuse in fuseObjects)
        {
            if (TileAt(fuse.X, fuse.Y) is not { } tile)
                continue;
            tile.AddFuseObject(fuse);
            AdjustNeighbouringTiles(fuse);
        }
    }

    public SnowWarArena Arena { get; }
    public int Width { get; }
    public int Height { get; }

    public IReadOnlyList<SnowWarGameObject> Objects => _objects;

    public Tile? TileAt(int x, int y) => x < 0 || x >= Width || y < 0 || y >= Height ? null : _tiles[y][x];

    public SnowWarGameObject? GetObject(int id) => _objectsById.GetValueOrDefault(id);

    public void AddObject(SnowWarGameObject gameObject)
    {
        _objects.Add(gameObject);
        _objectsById[gameObject.Id] = gameObject;
        gameObject.IsActive = true;
    }

    public void RemoveObject(int id)
    {
        if (!_objectsById.Remove(id, out var gameObject))
            return;
        _objects.Remove(gameObject);
        gameObject.OnRemove();
    }

    public void PutOnDeleteList(SnowWarGameObject gameObject)
    {
        _toDelete.Add(gameObject);
        gameObject.IsActive = false;
    }

    public bool CollidesWithGround(SnowWarGameObject gameObject)
    {
        var location = gameObject.Location!;
        if (location.Z < 1)
            return true;
        return TileAt(Tile.ToTileX(location.X), Tile.ToTileY(location.Y)) is { } tile && location.Z < tile.Height;
    }

    /// <summary>Everything moves once, then what was put aside goes.</summary>
    public void Subturn()
    {
        foreach (var gameObject in _objects.ToList())
            gameObject.Subturn(this);

        if (_toDelete.Count == 0)
            return;
        foreach (var gameObject in _toDelete)
            RemoveObject(gameObject.Id);
        _toDelete.Clear();
    }

    /// <summary>The client's checksum of a turn: the turn's seed plus, for every active object, each variable times
    /// its place (counting from one).</summary>
    public int Checksum(int turn)
    {
        unchecked
        {
            var sum = SnowWarMath.IterateSeed(turn);
            foreach (var gameObject in _objects)
            {
                if (!gameObject.IsActive)
                    continue;
                for (var i = 0; i < gameObject.VariableCount; i++)
                    sum += gameObject.GetVariable(i) * (i + 1);
            }

            return sum;
        }
    }

    private void LinkIfThere(Tile tile, int x, int y, Direction8 direction)
    {
        if (TileAt(x, y) is { } neighbour)
            tile.Link(neighbour, direction);
    }

    /// <summary>A furni wider than a tile raises, and unless one may stand on it blocks, the other tiles it covers.</summary>
    private void AdjustNeighbouringTiles(SnowWarFuseObject fuse)
    {
        var (xSize, ySize) = fuse.Direction == Direction8.E.Value || fuse.Direction == Direction8.W.Value
            ? (fuse.YDimension, fuse.XDimension)
            : (fuse.XDimension, fuse.YDimension);
        for (var i = 1; i < xSize; i++)
            Cover(TileAt(fuse.X + i, fuse.Y), fuse);
        for (var i = 1; i < ySize; i++)
            Cover(TileAt(fuse.X, fuse.Y + i), fuse);
    }

    private static void Cover(Tile? tile, SnowWarFuseObject fuse)
    {
        if (tile is null)
            return;
        tile.AddToHeight(fuse.Height);
        if (!fuse.CanStandOn)
            tile.Blocked = true;
    }
}
