using Habbo.Domain.Furniture;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>Decorating: a wallpaper, floor or landscape from the inventory is put down in the room, in place of the
/// one there, and is used up.</summary>
public sealed partial class RoomGrain
{
    /// <summary>The room properties the client draws the walls, floor and view outside from.</summary>
    private const string WallpaperProperty = "wallpaper";
    private const string FloorProperty = "floor";
    private const string LandscapeProperty = "landscape";

    public async Task DecorateAsync(Guid playerId, int itemId)
    {
        if (_room is null || !IsController(playerId))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.UserItems.Include(i => i.Definition)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.OwnerId == playerId && i.RoomId == null && !i.InMarketplace && i.ChestId == null);
        if (row?.Definition is not { } definition)
            return;

        // the pattern is the item's data: the number the shop gave it
        var pattern = (await FurniDataStore.LoadAsync(db, [row])).GetValueOrDefault(row.Id)?.State;
        if (string.IsNullOrEmpty(pattern))
            return;

        var room = await db.Rooms.FirstAsync(r => r.Id == _room.Id);
        string property, achievement;
        switch (FurniCategory.Of(definition))
        {
            case FurniCategory.WallPaper:
                (property, achievement) = (WallpaperProperty, AchievementNames.RoomDecoWallpaper);
                _room.Wallpaper = room.Wallpaper = pattern;
                break;
            case FurniCategory.Floor:
                (property, achievement) = (FloorProperty, AchievementNames.RoomDecoFloor);
                _room.FloorType = room.FloorType = pattern;
                break;
            case FurniCategory.Landscape:
                (property, achievement) = (LandscapeProperty, AchievementNames.RoomDecoLandscape);
                _room.Landscape = room.Landscape = pattern;
                break;
            default:
                return;
        }

        db.UserItems.Remove(row);
        await db.SaveChangesAsync();

        SendTo(playerId, FurniListRemoveComposer.Item(itemId));
        Broadcast(new RoomPropertyMessageComposer(property, pattern));
        GrainFactory.GetGrain<IPlayerGrain>(playerId).ProgressAchievementAsync(achievement, 1).Ignore();
    }
}
