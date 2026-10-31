using System.Globalization;
using Habbo.Domain.Furniture;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Items.Interactions;

namespace Habbo.Silo.Rooms;

/// <summary>Owns a room's furniture: footprints, placement validation, stacking state and
/// the wire projections. The grain handles messaging; this handles items.</summary>
public sealed class RoomItemStore(RoomModel model, FurniInteractionRegistry interactions)
{
    /// <summary>How close two surfaces have to be to count as the same level.</summary>
    private const double HeightTolerance = 0.01;

    /// <summary>The room's items by the id the room and its visitors know them by (<see cref="PlacedItem.Id"/>).</summary>
    private readonly Dictionary<int, PlacedItem> _items = [];

    /// <summary>The room's items by their <see cref="UserItem"/>'s id: the database's, for what is saved or read back
    /// under it (a wired copy, having no row, goes by its own).</summary>
    private readonly Dictionary<int, PlacedItem> _stored = [];

    /// <summary>The floor items covering each tile. Every question about a tile (what blocks it, what stands on
    /// top, where to sit) asks this, and pathfinding asks it of every tile it reaches, so it is kept rather than
    /// worked out by going through every item in the room. Kept up to date by Load, Add, Remove and MoveTo, the
    /// only ways a floor item comes, goes or moves.</summary>
    private readonly Dictionary<(int X, int Y), List<PlacedItem>> _tiles = [];

    private void Index(PlacedItem item)
    {
        if (item.Item.Definition!.Type != ItemType.Floor)
            return;

        foreach (var tile in Footprint(item))
        {
            if (!_tiles.TryGetValue(tile, out var here))
                _tiles[tile] = here = [];
            here.Add(item);
        }
    }

    private void Unindex(PlacedItem item)
    {
        if (item.Item.Definition!.Type != ItemType.Floor)
            return;

        foreach (var tile in Footprint(item))
        {
            if (_tiles.TryGetValue(tile, out var here) && here.Remove(item) && here.Count == 0)
                _tiles.Remove(tile);
        }
    }

    public IReadOnlyCollection<PlacedItem> Items => _items.Values;

    /// <summary>The item the room knows by this id: what the client names an item by, and what the room's own
    /// bookkeeping (wired, games, chests) keys it by.</summary>
    public PlacedItem? Find(int itemId) => _items.GetValueOrDefault(itemId);

    /// <summary>The item whose database row has this id: for what was saved under it (wired picks, teleport links,
    /// an inventory item being put down). A wired copy, which has no row, is found by its own id.</summary>
    public PlacedItem? FindStored(int databaseId) => _stored.GetValueOrDefault(databaseId);

    /// <summary>The room-facing id of the item saved under a database id, if it is here; otherwise the id as it is,
    /// which is what any other item goes by anyway.</summary>
    public int IdOfStored(int databaseId) => FindStored(databaseId)?.Id ?? databaseId;

    /// <summary>The database id of the item the room knows by this id, if it is here; otherwise the id as it is.</summary>
    public int StoredIdOf(int itemId) => Find(itemId)?.Item.Id ?? itemId;

    /// <summary>The first id of Builders Club furni: Habbo's client tells borrowed furni by its id alone
    /// (FurniId.isBuilderClubId: 2147418112 to 2147483647), and shows it with the club's border and owner line.</summary>
    public const int FirstBuildersClubId = 2147418112;

    private int _nextBuildersClubId = FirstBuildersClubId;

    /// <summary>The Builders Club ids of items gone from the room, oldest first: used again once every fresh one is.</summary>
    private readonly Queue<int> _freedBuildersClubIds = [];

    /// <summary>The id the room gives an item coming in: Builders Club furni a room-local id in the club's range (the
    /// one it has already, if it is here); anything else its database id. habbo.com (25 October 2026) sent the first
    /// Builders Club item put down in a room as 2147418112 (0x7FFF0000), on its ObjectAdd and again in the room's
    /// objects on entry, and the client clicked, used and opened its wired dialog by that id. The ids are taken to
    /// count up by one per room, in the order the room loads or places its Builders Club furni: one capture is all
    /// there is to go by. An item keeps its id for the room's life; one picked up is not used again until every
    /// fresh one has been.</summary>
    private int IdFor(UserItem item)
    {
        if (!item.BuildersClub)
            return item.Id;
        if (_stored.TryGetValue(item.Id, out var here))
            return here.Id;
        if (_nextBuildersClubId < int.MaxValue)
            return _nextBuildersClubId++;
        return _freedBuildersClubIds.TryDequeue(out var freed) ? freed : int.MaxValue;
    }

    /// <summary>Puts an item with a database row in the room under its id, in place of whatever was there under
    /// either of its ids.</summary>
    private void Store(PlacedItem placed)
    {
        if (_stored.Remove(placed.Item.Id, out var earlier) && _items.Remove(earlier.Id))
            Unindex(earlier);
        if (_items.Remove(placed.Id, out var other))
            Unindex(other);
        _items[placed.Id] = placed;
        _stored[placed.Item.Id] = placed;
    }

    public void Load(IEnumerable<UserItem> items, IReadOnlyDictionary<int, FurniDataView> data)
    {
        foreach (var item in items)
        {
            var placed = new PlacedItem
            {
                Id = IdFor(item),
                Item = item,
                Data = data.GetValueOrDefault(item.Id, FurniDataView.Plain),
                Interaction = interactions.For(item.Definition!),
                X = item.X ?? 0,
                Y = item.Y ?? 0,
                Z = item.Z ?? 0,
                Rotation = item.Rotation ?? 0,
            };
            Store(placed);
            Index(placed);
        }
    }

    public PlacedItem Add(UserItem item, FurniDataView data, int x, int y, int rotation)
    {
        var placed = new PlacedItem
        {
            Id = IdFor(item),
            Item = item,
            Data = data,
            Interaction = interactions.For(item.Definition!),
            X = x,
            Y = y,
            Z = SurfaceAt(x, y),
            Rotation = rotation,
        };
        Store(placed);
        Index(placed);
        return placed;
    }

    /// <summary>The first id of wired's temporary copies: above any real item's, as Habbo numbers them.</summary>
    public const int FirstTemporaryId = 2147401728;

    /// <summary>The last id of wired's temporary copies: the client takes any above for Builders Club furni
    /// (FurniId.isTempId: 2147401728 to 2147418111).</summary>
    public const int LastTemporaryId = FirstBuildersClubId - 1;

    private int _nextTemporaryId = FirstTemporaryId;

    /// <summary>The ids of copies taken away, oldest first: used again once every fresh one is.</summary>
    private readonly Queue<int> _freedTemporaryIds = [];

    /// <summary>A temporary copy of a floor item, standing at (x, y) at the altitude given or on what is there; null
    /// while as many copies stand as there are ids for them.</summary>
    public PlacedItem? AddCopy(PlacedItem template, FurniDataView data, int x, int y, double? z, int rotation)
    {
        int id;
        if (_nextTemporaryId <= LastTemporaryId)
            id = _nextTemporaryId++;
        else if (!_freedTemporaryIds.TryDequeue(out id))
            return null;

        var item = new UserItem
        {
            Id = id,
            Definition = template.Item.Definition,
            DefinitionId = template.Item.DefinitionId,
            OwnerId = template.Item.OwnerId,
            Owner = template.Item.Owner,
            Extra = template.Item.Extra,
            Tradeable = false,
            Recyclable = false,
            Sellable = false,
        };
        var placed = new PlacedItem
        {
            Id = id,
            Item = item,
            Data = data,
            Interaction = interactions.For(item.Definition!),
            X = x,
            Y = y,
            Z = z ?? SurfaceAt(x, y),
            Rotation = rotation,
            Temporary = true,
        };
        _items[placed.Id] = placed;
        _stored[placed.Item.Id] = placed;
        Index(placed);
        return placed;
    }

    /// <summary>Wall items live on a wall position string, not on floor tiles.</summary>
    public PlacedItem AddWall(UserItem item, FurniDataView data)
    {
        var placed = new PlacedItem
        {
            Id = IdFor(item),
            Item = item,
            Data = data,
            Interaction = interactions.For(item.Definition!),
            X = 0,
            Y = 0,
            Z = 0,
            Rotation = 0,
        };
        Store(placed);
        return placed;
    }

    /// <summary>Areas hidden by the room's area hiders, as the floor map carries them.</summary>
    public List<AreaHideData> AreaHides() =>
        _items.Values.Where(item => item.Interaction is AreaHideInteraction)
            .Select(item => AreaHideInteraction.Area(item.Id, item.Data))
            .ToList();

    /// <summary>Takes the item the room knows by this id out of the room.</summary>
    public bool Remove(int itemId)
    {
        if (!_items.Remove(itemId, out var item))
            return false;

        Unindex(item);
        _stored.Remove(item.Item.Id);
        if (item.Temporary)
            _freedTemporaryIds.Enqueue(itemId);
        else if (item.Item.BuildersClub)
            _freedBuildersClubIds.Enqueue(itemId);
        return true;
    }

    /// <summary>Moves an item, settling it on what is there — or at the altitude given.</summary>
    public void MoveTo(PlacedItem item, int x, int y, int rotation, double? altitude = null)
    {
        var placed = _items.GetValueOrDefault(item.Id) == item;
        if (placed)
            Unindex(item);
        item.X = x;
        item.Y = y;
        item.Z = altitude ?? SurfaceAt(x, y, item.Id);
        item.Rotation = rotation;
        if (placed)
            Index(item);
    }

    /// <summary>How many tiles an item covers each way. Dimensions swap at 90°/270°.</summary>
    public static (int Width, int Height) FootprintSize(ItemDefinition definition, int rotation) =>
        rotation % 4 == 2 ? (definition.YDim, definition.XDim) : (definition.XDim, definition.YDim);

    /// <summary>The tiles an item covers.</summary>
    public static IEnumerable<(int X, int Y)> Footprint(PlacedItem item)
    {
        var (width, height) = FootprintSize(item.Item.Definition!, item.Rotation);
        for (var dx = 0; dx < width; dx++)
        {
            for (var dy = 0; dy < height; dy++)
                yield return (item.X + dx, item.Y + dy);
        }
    }

    /// <summary>True when the tile cannot be walked on at all — neither crossed nor stopped on.
    /// Anything an avatar can use its body on is walked straight through: a capture of a double
    /// bed moved into somebody's path has them cross its tiles without pausing. Only the topmost
    /// item decides, so a minibar standing on a rug blocks the tile the rug alone would not.
    /// Stopping on one of these is what sits or lies the avatar down, not crossing it.</summary>
    public bool BlocksWalking(int x, int y, bool clubMember = false, IReadOnlySet<int>? groupIds = null) =>
        TopItemAt(x, y) is { } item
        && item.Interaction switch
        {
            // a gate lets through while it is open, a club gate only club members, a group gate only its group's
            GateInteraction => item.Data.State != GateInteraction.Open,
            VipGateInteraction => !clubMember,
            GroupGateInteraction => !GroupGateInteraction.LetsThrough(item.Item.GroupId, groupIds),
            // a whole Freeze block stands in the way (Habbo); broken into a power-up it is stepped on
            FreezeBlockInteraction => FreezeBlockInteraction.Whole(item.Data),
            _ => item.Item.Definition! is not ({ CanWalk: true } or { CanSit: true } or { CanLay: true }),
        };

    /// <summary>Whether an avatar may stand on the tile: it is floor, and nothing on it keeps them off.</summary>
    public bool CanStandOn(int x, int y) => model.IsTile(x, y) && !BlocksWalking(x, y);

    /// <summary>Where one lies on the bed topping this tile: its head end, the bed's own row (lengthwise along y) or
    /// column (along x), in the lane of the tile. Null when no bed tops it.</summary>
    public (int X, int Y)? BedHeadFor(int x, int y) =>
        TopItemAt(x, y) is { Item.Definition.CanLay: true } bed
            ? bed.Rotation % 4 == 0 ? (x, bed.Y) : (bed.X, y)
            : null;

    /// <summary>Whether what tops the tile is sat or lain on, not walked over: a walk may end there but not pass.</summary>
    public bool SeatOrBedAt(int x, int y) =>
        TopItemAt(x, y)?.Item.Definition is { CanWalk: false } definition && (definition.CanSit || definition.CanLay);

    /// <summary>Whether a gate shut on the tile: a walk planned while it was open goes round it, or stops.</summary>
    public bool ClosedGateAt(int x, int y) =>
        TopItemAt(x, y) is { Interaction: GateInteraction } gate && gate.Data.State != GateInteraction.Open;

    /// <summary>The height anything arriving on this tile rests at: the top of whatever already
    /// stands there, or the floor when it is clear. <paramref name="ignoreItemId"/> leaves one item
    /// out, so an item being moved does not stack on top of itself.</summary>
    public double SurfaceAt(int x, int y, int? ignoreItemId = null)
    {
        // a stack helper decides the height on its tiles, whatever else is there
        if (StackHelperAt(x, y, ignoreItemId) is { StackHeight: { } stackHeight } helper)
            return helper.Z + stackHeight;

        var floor = model.GetHeight(x, y);
        return Math.Max(floor, FloorItemsAt(x, y, ignoreItemId).Select(TopOf).DefaultIfEmpty(floor).Max());
    }

    /// <summary>Whether nothing further may be put on this tile, because what is on top of it does
    /// not carry furniture. The client refuses the placement by this, so it has to agree with
    /// <see cref="CanPlaceFloorItem"/>.</summary>
    public bool StackingBlockedAt(int x, int y, int? ignoreItemId = null) =>
        StackHelperAt(x, y, ignoreItemId) is not { StackHeight: not null }
        && TopItemAt(x, y, ignoreItemId) is { } item && !item.Item.Definition!.CanStackOn;

    private PlacedItem? StackHelperAt(int x, int y, int? ignoreItemId) =>
        FloorItemsAt(x, y, ignoreItemId).FirstOrDefault(item => item.Interaction is StackHelperInteraction);

    /// <summary>The item whose surface the tile's is, if anything stands there.</summary>
    public PlacedItem? TopItemAt(int x, int y, int? ignoreItemId = null) =>
        FloorItemsAt(x, y, ignoreItemId).MaxBy(TopOf);

    /// <summary>Every tile of the room as the client's height map needs it. Void tiles are left
    /// out; the client reads a missing tile as one that is not part of the room.</summary>
    public List<HeightMapTileData> TileMap()
    {
        var tiles = new List<HeightMapTileData>(model.SizeX * model.SizeY);
        for (var y = 0; y < model.SizeY; y++)
        {
            for (var x = 0; x < model.SizeX; x++)
            {
                if (model.IsTile(x, y))
                    tiles.Add(HeightMapTile(x, y));
            }
        }

        return tiles;
    }

    /// <summary>The height an avatar walks at on this tile. Only furniture it can stand on lifts
    /// it: something it would sit or lie on is not underfoot while walking past, and a capture of
    /// a double bed (2x3, 1.8 high) moved into somebody's path has them step onto its tiles at
    /// 0.0 all the same. Sitting and lying carry their own height in the status instead.</summary>
    /// <summary>The height an avatar lies at on the tile, when a bed covers it; null otherwise.</summary>
    public double? BedHeightAt(int x, int y) => BedAt(x, y) is { } bed ? TopOf(bed) : null;

    /// <summary>The bed an avatar lies on at the tile: the highest, when beds are stacked.</summary>
    public PlacedItem? BedAt(int x, int y) =>
        FloorItemsAt(x, y, null).Where(item => item.Item.Definition!.CanLay).MaxBy(TopOf);

    public double AvatarHeightAt(int x, int y)
    {
        var floor = model.GetHeight(x, y);
        return Math.Max(floor, FloorItemsAt(x, y, null)
            .Where(item => item.Item.Definition!.CanWalk)
            .Select(TopOf)
            .DefaultIfEmpty(floor)
            .Max());
    }

    /// <summary>Where an avatar sits on this tile, if anything there is sittable. A chair standing
    /// on a table seats whoever uses it at the table's height plus the chair's own.</summary>
    public double? SeatHeightAt(int x, int y) => SeatAt(x, y) is { } seat ? TopOf(seat) : null;

    /// <summary>The seat an avatar sits on at the tile: the highest, when seats are stacked.</summary>
    public PlacedItem? SeatAt(int x, int y) =>
        FloorItemsAt(x, y, null).Where(item => item.Item.Definition!.CanSit).MaxBy(TopOf);

    /// <summary>The floor items covering a tile. Callers pick among them by height.</summary>
    private IEnumerable<PlacedItem> FloorItemsAt(int x, int y, int? ignoreItemId) =>
        _tiles.TryGetValue((x, y), out var here) ? here.Where(item => item.Id != ignoreItemId) : [];

    /// <summary>The floor items covering a tile, lowest first.</summary>
    /// <summary>The highest a floor item may stand, as wired's altitude slider reaches: 80.</summary>
    public const double MaxAltitude = 80.0;

    public IEnumerable<PlacedItem> FloorItemsOn(int x, int y) => FloorItemsAt(x, y, null).OrderBy(item => item.Z);

    /// <summary>The surface an item offers: where it stands, plus how tall it is.</summary>
    public static double TopOf(PlacedItem item) => item.Z + item.Item.Definition!.StackHeight;

    /// <summary>Validates a floor placement: a rotation the item has, inside the model, off the
    /// door, clear of avatars, and standing level — on the floor, or on top of furniture that
    /// carries other furniture. Every tile it covers has to offer the same surface, or part of
    /// the item would hang in the air. An avatar only blocks a tile the item does not cover
    /// already, so a chair can be rotated with someone sitting on it.</summary>
    public bool CanPlaceFloorItem(
        int itemId, ItemDefinition definition, int x, int y, int rotation, Func<int, int, bool> hasAvatar,
        bool throughFurni = false, bool blockedByFurni = false, bool anyDirection = false)
    {
        // A rotation the SWF has no artwork for draws nothing at all; a game's ball faces the way it rolls whatever
        // its artwork (Habbo sends a football at 4 and 6, which has 0 to 3)
        if (!anyDirection && !definition.AllowsDirection(rotation))
            return false;

        var covered = Find(itemId) is { } placed ? Footprint(placed).ToHashSet() : [];
        var (width, height) = FootprintSize(definition, rotation);
        var surface = SurfaceAt(x, y, itemId);
        for (var dx = 0; dx < width; dx++)
        {
            for (var dy = 0; dy < height; dy++)
            {
                var tx = x + dx;
                var ty = y + dy;
                if (!model.IsTile(tx, ty))
                    return false;
                if (tx == model.DoorX && ty == model.DoorY)
                    return false;
                if (hasAvatar(tx, ty) && !covered.Contains((tx, ty)))
                    return false;
                if (blockedByFurni && FloorItemsOn(tx, ty).Any(other => other.Id != itemId))
                    return false;
                if (throughFurni)
                    continue;
                if (StackingBlockedAt(tx, ty, itemId))
                    return false;
                if (Math.Abs(SurfaceAt(tx, ty, itemId) - surface) > HeightTolerance)
                    return false;
            }
        }

        return true;
    }

    /// <summary>Recomputes the border mask every water area carries as its state and returns the
    /// ones it changed. A water area's shore is drawn from what surrounds it, so placing, moving
    /// or picking up any item that borders one can change more than the item that moved.</summary>
    public List<PlacedItem> RefreshWaterAreas()
    {
        var changed = new List<PlacedItem>();
        foreach (var item in _items.Values.Where(item => item.Item.Definition!.IsWaterArea))
        {
            var (width, height) = FootprintSize(item.Item.Definition!, item.Rotation);
            var mask = WaterAreaBorderMask.For(
                item.X, item.Y, width, height, model.IsTile,
                (x, y) => IsWaterAreaAt(x, y, item.Id));

            var state = mask.ToString(CultureInfo.InvariantCulture);
            if (item.Data.StateText == state)
                continue;

            item.Data = FurniDataView.WithState(state);
            changed.Add(item);
        }

        return changed;
    }

    /// <summary>Whether another water area covers the tile, so the two share a surface and
    /// neither draws a shore between them.</summary>
    private bool IsWaterAreaAt(int x, int y, int exceptItemId) =>
        FloorItemsAt(x, y, exceptItemId).Any(item => item.Item.Definition!.IsWaterArea);

    /// <summary>The height-map rows for an item's tiles after a placement or pickup.</summary>
    public List<HeightMapTileData> TileUpdates(IEnumerable<(int X, int Y)> tiles) =>
        tiles
            .Distinct()
            .Select(tile => HeightMapTile(tile.X, tile.Y))
            .ToList();

    /// <summary>A tile as the client's stacking map has it. One whose top item carries nothing is blocked, at the
    /// height that item stands on, as Habbo sends it (a sandwich on a 0.45 roller: 0.45, not 0.85; furniture on
    /// the floor: 0.0); any other is at its surface.</summary>
    private HeightMapTileData HeightMapTile(int x, int y) =>
        StackingBlockedAt(x, y) && TopItemAt(x, y) is { } top
            ? new HeightMapTileData(x, y, top.Z, StackingBlocked: true)
            : new HeightMapTileData(x, y, SurfaceAt(x, y), StackingBlocked: false);

    public List<RoomItemData> Project(ItemType type) =>
        _items.Values
            .Where(item => item.Item.Definition!.Type == type)
            .Select(ToWireData)
            .ToList();

    public static RoomItemData ToWireData(PlacedItem item)
    {
        var definition = item.Item.Definition!;
        return new RoomItemData(
            item.Id,
            definition.ClassId,
            definition.Type == ItemType.Wall,
            item.X,
            item.Y,
            FormatHeight(item.Z),
            item.Rotation,
            FormatHeight(definition.SizeZ),
            item.Interaction.InRoom(item.Data),
            item.Item.WallPosition ?? string.Empty,
            item.Item.Owner?.AccountId ?? 0,
            item.Item.Owner?.Username ?? string.Empty,
            item.Item.Extra,
            item.Item.ExpiryTime,
            (int)item.Interaction.UsagePolicy);
    }

    /// <summary>A height as Habbo writes it: at least one decimal, at most two ("0.0", "0.45", "3.0"). One decimal
    /// made a roller's 0.45 a 0.5.</summary>
    public static string FormatHeight(double value) => value.ToString("0.0#", CultureInfo.InvariantCulture);

    /// <summary>An item placed in the room, with its live position.</summary>
    public sealed class PlacedItem
    {
        /// <summary>The id the room and the client know the item by. It is the database id (<c>Item.Id</c>) but for
        /// Builders Club furni, which goes by a room-local id in the club's range, and wired's temporary copies, by
        /// one in theirs. Everything the room sends that names the item names it by this, the client names it by
        /// this, and the room keeps track of it by this; what is saved goes by the database id.</summary>
        public required int Id { get; init; }

        public required UserItem Item { get; init; }

        /// <summary>The item's data as the room last knew it. Settable because using an item
        /// changes it — a dice rolled, a lamp switched on.</summary>
        public required FurniDataView Data { get; set; }
        // moved only through the store's MoveTo, which keeps its tile index with them
        public required int X { get; set; }
        public required int Y { get; set; }
        public required double Z { get; set; }
        public required int Rotation { get; set; }

        /// <summary>What the item does. Resolved once when the item enters the room.</summary>
        public required FurniInteraction Interaction { get; init; }

        /// <summary>Until when the item is in the middle of something — a dice rolling, a wheel
        /// spinning — and ignores further use. Kept here rather than in the item's data so a
        /// restart mid-roll cannot leave it stuck.</summary>
        public DateTime BusyUntil { get; set; }

        public bool IsBusy => DateTime.UtcNow < BusyUntil;

        /// <summary>For a stack helper: the height things put on its tiles stand at, above it.</summary>
        public double? StackHeight { get; set; }

        /// <summary>A copy wired placed: it lives in the room only, is never saved, and goes when wired removes it or
        /// the room unloads.</summary>
        public bool Temporary { get; init; }

        /// <summary>For a stack helper: whether avatars walk across its tiles at every height (its multi-walk mode).</summary>
        public bool MultiWalk { get; set; }

        /// <summary>How wired's set visibility left it for everyone; null as it was placed. Kept for the room's life, not saved.</summary>
        public WiredTransparency? Transparency { get; set; }

        /// <summary>Made invisible for everyone (habbo.com's @is_invisible).</summary>
        public bool Invisible => Transparency is { Transparency: WiredTransparency.Invisible };

        /// <summary>Given @can_move_freely by wired: its wired moves pass through furni and avatars. Kept for the room's life.</summary>
        public bool MovesFreely { get; set; }

        /// <summary>Its last flight as a wired projectile, and what it ran into on the way; null until it first flies.</summary>
        public ProjectileFlight? Projectile { get; set; }
    }
}
