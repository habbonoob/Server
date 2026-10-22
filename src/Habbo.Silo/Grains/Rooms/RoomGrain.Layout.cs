using Habbo.Domain.Rooms;
using Habbo.Domain.Furniture;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.Room.Layout;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The floor plan editor: what it needs to know of the room, and saving a new plan, which
/// rebuilds the room around everyone in it.</summary>
public sealed partial class RoomGrain
{
    /// <summary>The notification the client words a rejected plan with; its text lists the failures.</summary>
    private const string FloorPlanErrorNotification = "floorplan_editor.error";

    private const string FloorPlanErrorsParameter = "ERRORS";

    private const string FloorPlanErrorKeyPrefix = "${notification.floorplan_editor.error.message.";

    /// <summary>The perk that lifts the editor's area limit.</summary>
    private const string LargeFloorPlansPerk = "BUILDER_AT_WORK";

    /// <summary>What a room's own plan is called: it is the room's alone, so no new room is made on it.</summary>
    internal const string CustomModelPrefix = "custom_";

    public Task SendOccupiedTilesAsync(Guid playerId)
    {
        if (_items is not null)
            SendTo(playerId, new RoomOccupiedTilesMessageComposer(OccupiedTiles().Select(t => new OccupiedTileData(t.X, t.Y)).ToList()));
        return Task.CompletedTask;
    }

    private List<(int X, int Y)> OccupiedTiles() =>
        _items!.Items.Where(item => item.Item.Definition?.Type == ItemType.Floor)
            .SelectMany(RoomItemStore.Footprint).Distinct().ToList();

    public Task SendEntryTileAsync(Guid playerId)
    {
        if (_room?.Model is { } model)
            SendTo(playerId, new RoomEntryTileMessageComposer(model.DoorX, model.DoorY, model.DoorDirection));
        return Task.CompletedTask;
    }

    public async Task UpdateFloorPlanAsync(PlayerData player, string heightmap, int? doorX, int? doorY, int? doorDirection,
        int? wallThickness, int? floorThickness, int? fixedWallsHeight)
    {
        if (_room?.Model is not { } model || _model is null || _entities is null || !MayManage(player))
            return;

        var plan = new FloorPlan(heightmap,
            doorX ?? model.DoorX, doorY ?? model.DoorY, doorDirection ?? model.DoorDirection,
            wallThickness ?? _room.WallThicknessExponent, floorThickness ?? _room.FloorThicknessExponent,
            fixedWallsHeight ?? _room.FixedWallsHeight);
        var largePlansAllowed = (await perks.GetAllowancesAsync(player))
            .Any(perk => perk.Code == LargeFloorPlansPerk && perk.IsAllowed);
        var errors = FloorPlanValidator.Validate(plan, roomOptions.Value, largePlansAllowed, OccupiedTiles(), _model, out var tidied);
        if (errors.Count > 0)
        {
            SendTo(player.Id, new NotificationDialogMessageComposer(FloorPlanErrorNotification,
                [new NotificationParameterData(FloorPlanErrorsParameter,
                    string.Join("<br>", errors.Select(error => FloorPlanErrorKeyPrefix + error + "}")))]));
            return;
        }

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var customName = CustomModelPrefix + _room.Id;
            var custom = await db.RoomModels.FirstOrDefaultAsync(m => m.Name == customName);
            if (custom is null)
                db.RoomModels.Add(custom = new RoomModelData { Name = customName, Heightmap = tidied });

            custom.Heightmap = tidied;
            custom.DoorX = plan.DoorX;
            custom.DoorY = plan.DoorY;
            custom.DoorDirection = plan.DoorDirection;
            await db.SaveChangesAsync();

            await db.Rooms.Where(r => r.Id == _room.Id).ExecuteUpdateAsync(s => s
                .SetProperty(r => r.ModelId, custom.Id)
                .SetProperty(r => r.WallThicknessExponent, plan.WallThickness)
                .SetProperty(r => r.FloorThicknessExponent, plan.FloorThickness)
                .SetProperty(r => r.FixedWallsHeight, plan.FixedWallsHeight));
        }

        // everyone comes back in to the rebuilt room, which loads afresh
        foreach (var entity in _entities.Entities.ToList())
        {
            SendTo(entity.Player.Id, new RoomForwardMessageComposer(_room.Id));
            await LeaveAsync(entity.Player.Id);
        }

        DeactivateOnIdle();
    }
}
