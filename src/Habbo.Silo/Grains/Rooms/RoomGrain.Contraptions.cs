using Habbo.Silo.Abstractions.Permissions;
using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Furniture;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;
using Habbo.Silo.Permissions;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Furniture with a dialog or a trick of its own: the one-way door, the wheel, the area hider,
/// the stack helper, and the data staff edit on any item.</summary>
public sealed partial class RoomGrain
{
    /// <summary>What the stack helper's "on top of the stack" asks for.</summary>
    private const int OnTopOfStack = -100;

    /// <summary>The stack helper's height comes in hundredths of a tile.</summary>
    private const double HundredthsPerTile = 100.0;

    /// <summary>The most keys an item's map data set from the info stand holds. The client's own dialogs set a handful
    /// (an ad's image, link and offsets); this leaves staff room for more while keeping what everyone in the room is sent
    /// small.</summary>
    private const int MaxObjectDataEntries = 32;

    /// <summary>How long the door stays open while the avatar steps through.</summary>
    private static readonly TimeSpan DoorStepTime = TimeSpan.FromMilliseconds(500);

    public async Task EnterOneWayDoorAsync(Guid playerId, int itemId)
    {
        if (_entities?.Find(playerId) is not { } entity || _items?.Find(itemId) is not { Interaction: OneWayDoorInteraction } door
            || door.IsBusy)
            return;

        // the door lets through from the side it faces to the side behind it
        var (dx, dy) = DirectionStep(door.Rotation);
        var (frontX, frontY) = (door.X + dx, door.Y + dy);
        var (backX, backY) = (door.X - dx, door.Y - dy);

        await WalkThenAsync(entity, frontX, frontY, () =>
        {
            if (entity.X != frontX || entity.Y != frontY || !_furniture!.IsPlaced(door) || _entities.HasAvatarAt(door.X, door.Y))
                return Task.CompletedTask;

            door.BusyUntil = DateTime.UtcNow + DoorStepTime * 2;
            Broadcast(new OneWayDoorStatusMessageComposer(itemId, OneWayDoorInteraction.Open));
            _entities.Carry(entity, door.X, door.Y);
            Broadcast(UserUpdateMessageComposer.From([_entities.CurrentStatus(entity)]));

            Schedule(DoorStepTime, () =>
            {
                // through to the other side if it is free, back where they came from if not; the door shuts either way,
                // also behind one who left meanwhile
                if (_entities.Find(playerId) is not null
                    && (_entities.Slide(entity, backX, backY) ?? _entities.Slide(entity, frontX, frontY)) is { } through)
                    Broadcast(UserUpdateMessageComposer.From([through]));
                Broadcast(new OneWayDoorStatusMessageComposer(itemId, OneWayDoorInteraction.Closed));
                return Task.CompletedTask;
            });
            return Task.CompletedTask;
        });
    }

    /// <summary>The step a rotation faces: 0 north, 2 east, 4 south, 6 west.</summary>
    private static (int X, int Y) DirectionStep(int rotation) => (rotation % 8) switch
    {
        0 => (0, -1),
        1 => (1, -1),
        2 => (1, 0),
        3 => (1, 1),
        4 => (0, 1),
        5 => (-1, 1),
        6 => (-1, 0),
        _ => (-1, -1),
    };

    public async Task SpinWheelAsync(Guid playerId, int itemId)
    {
        if (_furniture is not null && _entities?.Find(playerId) is { } entity)
            await _furniture.UseAsync(entity, IsController(playerId), itemId, FurniAction.Spin, 0);
    }

    public async Task SetAreaHideAsync(Guid playerId, int itemId, int rootX, int rootY, int width, int length,
        bool invisibility, bool wallItems, bool invert)
    {
        if (!IsController(playerId) || _model is null || _items?.Find(itemId) is not { Interaction: AreaHideInteraction } hider
            || rootX < 0 || rootY < 0 || width < 0 || length < 0
            || rootX + width > _model.SizeX || rootY + length > _model.SizeY)
            return;

        var values = AreaHideInteraction.Values(hider.Data);
        values[AreaHideInteraction.RootXIndex] = rootX;
        values[AreaHideInteraction.RootYIndex] = rootY;
        values[AreaHideInteraction.WidthIndex] = width;
        values[AreaHideInteraction.LengthIndex] = length;
        values[AreaHideInteraction.InvisibilityIndex] = invisibility ? AreaHideInteraction.True : AreaHideInteraction.False;
        values[AreaHideInteraction.WallItemsIndex] = wallItems ? AreaHideInteraction.True : AreaHideInteraction.False;
        values[AreaHideInteraction.InvertIndex] = invert ? AreaHideInteraction.True : AreaHideInteraction.False;
        await _furniture!.UpdateAsync(hider, AreaHideInteraction.WithValues(hider.Data, values));
        BroadcastAreaHide(hider);
    }

    /// <summary>Tells the room the area a hider hides, which the floor map carries only on entry.</summary>
    private void BroadcastAreaHide(RoomItemStore.PlacedItem hider)
    {
        var area = AreaHideInteraction.Area(hider.Item.Id, hider.Data);
        Broadcast(new AreaHideMessageComposer(new AreaHideWireData(area.FurniId, area.On, area.RootX, area.RootY, area.Width, area.Length, area.Invert)));
    }

    public async Task SetStackHeightAsync(Guid playerId, int itemId, int height, bool? multiWalk)
    {
        if (!IsController(playerId) || _items?.Find(itemId) is not { Interaction: StackHelperInteraction } helper)
            return;

        // "on top of the stack" is the height of what else stands on its tiles
        var hundredths = height == OnTopOfStack
            ? (int)Math.Round((RoomItemStore.Footprint(helper).Max(t => _items.SurfaceAt(t.X, t.Y, helper.Item.Id)) - helper.Z) * HundredthsPerTile)
            : height;
        hundredths = Math.Clamp(hundredths, 0, (int)(RoomItemStore.MaxAltitude * HundredthsPerTile));

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var row = await db.FurniStackHeights.FirstOrDefaultAsync(s => s.ItemId == itemId);
            if (row is null)
                db.FurniStackHeights.Add(row = new FurniStackHeight { ItemId = itemId });
            row.Height = hundredths;
            row.MultiWalk = multiWalk ?? row.MultiWalk;
            await db.SaveChangesAsync();
        }

        helper.StackHeight = hundredths / HundredthsPerTile;
        BroadcastHeights(RoomItemStore.Footprint(helper));
        Broadcast(new CustomStackingHeightUpdateMessageComposer(itemId, hundredths));
    }

    /// <summary>Reads the heights of the room's stack helpers.</summary>
    private async Task LoadStackHeightsAsync(Habbo.Infrastructure.AppDbContext db)
    {
        var helperIds = _items!.Items.Where(i => i.Interaction is StackHelperInteraction).Select(i => i.Item.Id).ToList();
        if (helperIds.Count == 0)
            return;

        foreach (var row in await db.FurniStackHeights.AsNoTracking().Where(s => helperIds.Contains(s.ItemId)).ToListAsync())
            _items.Find(row.ItemId)!.StackHeight = row.Height / HundredthsPerTile;
    }

    /// <summary>An item's map data set from the info stand. Staff edit any item's; a room's owner the
    /// items whose kind the hotel lets owners set (Furniture:OwnerEditableDataLogics).</summary>
    public async Task SetObjectDataAsync(PlayerData player, int itemId, IReadOnlyList<string> keysAndValues)
    {
        if (_room is null || _items?.Find(itemId) is not { } item || keysAndValues.Count % 2 != 0)
            return;

        var definition = item.Item.Definition!;
        var mayEdit = permissions.Has(player, PermissionNames.RoomManageAny)
                      || (_room.OwnerId == player.Id && furnitureOptions.Value.OwnerEditableDataLogics.Contains(definition.Logic));
        var limit = furnitureOptions.Value.ObjectDataValueMaxLength;
        if (!mayEdit || keysAndValues.Count / 2 > MaxObjectDataEntries || keysAndValues.Any(text => text.Length > limit))
            return;

        var entries = Enumerable.Range(0, keysAndValues.Count / 2)
            .Select(i => new FurniMapEntryView(keysAndValues[i * 2], keysAndValues[i * 2 + 1]))
            .DistinctBy(e => e.Key)
            .ToList();
        await _furniture!.UpdateAsync(item, item.Data with { Type = (int)FurniDataType.Map, State = null, MapEntries = entries });
    }
}
