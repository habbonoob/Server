using System.Globalization;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The room's rollers. Every so many ticks — the room's default, or what a roller speed
/// controller is set to — each roller moves what is on it one tile on: the furniture stacked on it,
/// keeping its height above the roller, and at most one avatar standing there. Nothing moves onto a
/// tile it could not be put on, and nothing is moved twice in one turn.</summary>
public sealed partial class RoomGrain
{
    private int _ticksSinceRoll;

    /// <summary>The tiles the last roll moved furniture off and onto, whose heights the next tick tells.</summary>
    private readonly List<(int X, int Y)> _rolledTiles = [];

    private int RollerTicks =>
        _items?.Items
            .Where(item => item.Item.Definition!.ClassName == roomOptions.Value.RollerSpeedControllerClass)
            .Select(item => int.TryParse(item.Data.State, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks) ? ticks : (int?)null)
            .FirstOrDefault(ticks => ticks is not null)
        ?? roomOptions.Value.RollerTicks;

    private async Task RollAsync()
    {
        if (_items is null || _entities is null)
            return;

        // the heights the furniture rolled changes are told the tick after the slide, as Habbo does
        if (_rolledTiles.Count > 0)
        {
            BroadcastHeights(_rolledTiles);
            _rolledTiles.Clear();
        }

        // every RollerTicks-th tick: Habbo's rollers move every 4 ticks, 2 seconds apart
        if (++_ticksSinceRoll < RollerTicks)
            return;

        _ticksSinceRoll = 0;
        var rollers = _items.Items.Where(item => item.Interaction is RollerInteraction).ToList();
        if (rollers.Count == 0)
            return;

        var movedItems = new HashSet<int>();
        var movedUsers = new HashSet<Guid>();
        var moved = new List<RoomItemStore.PlacedItem>();

        foreach (var roller in rollers)
        {
            var (dx, dy) = DirectionStep(roller.Rotation);
            var (toX, toY) = (roller.X + dx, roller.Y + dy);
            if (_model is null || !_model.IsTile(toX, toY) || (toX == _model.DoorX && toY == _model.DoorY))
                continue;

            var rollerTop = RoomItemStore.TopOf(roller);
            var riders = _items.FloorItemsOn(roller.X, roller.Y)
                .Where(item => item != roller && item.Z >= rollerTop - 0.01 && !movedItems.Contains(item.Item.Id)
                               && RoomItemStore.FootprintSize(item.Item.Definition!, item.Rotation) == (1, 1))
                .ToList();
            var rider = _entities.Entities.FirstOrDefault(e => e.X == roller.X && e.Y == roller.Y && !e.Walking && !movedUsers.Contains(e.Player.Id));

            // what lands on the next tile lands on its roller, if it has one, or on what is there
            var nextRoller = _items.FloorItemsOn(toX, toY).FirstOrDefault(item => item.Interaction is RollerInteraction);
            var ignored = riders.Select(r => r.Item.Id).ToHashSet();
            var landing = nextRoller is not null ? RoomItemStore.TopOf(nextRoller) : _items.SurfaceAt(toX, toY);
            var furniFits = riders.Count > 0 && !_entities.HasAvatarAt(toX, toY)
                            && (nextRoller is not null || !_items.StackingBlockedAt(toX, toY))
                            && _items.FloorItemsOn(toX, toY).All(item => item.Interaction is RollerInteraction || ignored.Contains(item.Item.Id) || item.Item.Definition!.CanStackOn);

            var slid = new List<SlideObjectData>();
            if (furniFits)
            {
                foreach (var item in riders)
                {
                    var fromZ = item.Z;
                    var toZ = landing + (item.Z - rollerTop);
                    _items.MoveTo(item, toX, toY, item.Rotation, toZ);
                    movedItems.Add(item.Item.Id);
                    moved.Add(item);
                    slid.Add(new SlideObjectData(item.Item.Id, RoomItemStore.FormatHeight(fromZ), RoomItemStore.FormatHeight(toZ)));
                }
            }

            SlideObjectData? avatar = null;
            if (rider is not null && !_items.BlocksWalking(toX, toY) && !_entities.HasAvatarAt(toX, toY))
            {
                // at the height it is drawn at, so one sitting stays up through the slide
                var fromZ = _entities.DrawnHeight(rider);
                _entities.Roll(rider, toX, toY);
                movedUsers.Add(rider.Player.Id);
                // one riding a pet rolls with it: the two share a tile
                if (rider.Mount is { } mount)
                {
                    _entities.Carry(mount, toX, toY);
                    Broadcast(UserUpdateMessageComposer.From([_entities.CurrentStatus(mount)]));
                }
                avatar = new SlideObjectData(rider.RoomIndex, RoomItemStore.FormatHeight(fromZ),
                    RoomItemStore.FormatHeight(_entities.DrawnHeight(rider)));
                // the walk off and onto furni is the tick's to tell wired: it sees the rider moved, as it sees anyone who did
            }

            if (slid.Count == 0 && avatar is null)
                continue;

            // the heights change only where furniture moved: an avatar rolled alone changes none (Habbo sends none).
            // Habbo lists the tile rolled onto first.
            if (slid.Count > 0)
            {
                _rolledTiles.Add((toX, toY));
                _rolledTiles.Add((roller.X, roller.Y));
            }

            // one standing on a roller slides along in their posture: "mv" would leave them walking on the spot
            Broadcast(new SlideObjectBundleMessageComposer(roller.X, roller.Y, toX, toY, slid, roller.Item.Id,
                avatar is null ? null : SlideObjectBundleMessageComposer.AvatarSlides, avatar));
        }

        if (moved.Count == 0)
            return;

        // the rolled items' rows read and written back together: two round trips a roll, not one per item
        await using var db = await dbFactory.CreateDbContextAsync();
        var ids = moved.Select(item => item.Item.Id).ToList();
        var rows = await db.UserItems.Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
        foreach (var item in moved)
        {
            if (rows.TryGetValue(item.Item.Id, out var row))
                (row.X, row.Y, row.Z) = (item.X, item.Y, item.Z);
        }

        await db.SaveChangesAsync();
    }
}
