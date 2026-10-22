using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Items;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The wired menu's room state: reloading the room, everyone coming back in, and rolling its furniture back
/// first — every move and state change since it last loaded undone.</summary>
public sealed partial class RoomGrain
{
    /// <summary>Where each item stood, and what it showed, when the room loaded.</summary>
    private readonly Dictionary<int, (int X, int Y, double Z, int Rotation, string? WallPosition, FurniDataView Data)> _loadedFurni = [];

    private void RememberLoadedFurni()
    {
        _loadedFurni.Clear();
        foreach (var item in _items!.Items)
            _loadedFurni[item.Item.Id] = (item.X, item.Y, item.Z, item.Rotation, item.Item.WallPosition, item.Data);
    }

    public async Task WiredUpdateRoomAsync(Guid playerId, bool rollBack)
    {
        if (_room is null || _items is null || _entities?.Find(playerId) is not { } user || !WiredRights(user).Modify)
            return;

        if (rollBack)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            foreach (var item in _items.Items.ToList())
            {
                if (!_loadedFurni.TryGetValue(item.Item.Id, out var loaded))
                    continue;

                if (item.X != loaded.X || item.Y != loaded.Y || item.Z != loaded.Z || item.Rotation != loaded.Rotation
                    || item.Item.WallPosition != loaded.WallPosition)
                {
                    await db.UserItems.Where(i => i.Id == item.Item.Id).ExecuteUpdateAsync(set => set
                        .SetProperty(i => i.X, loaded.X).SetProperty(i => i.Y, loaded.Y).SetProperty(i => i.Z, loaded.Z)
                        .SetProperty(i => i.Rotation, loaded.Rotation).SetProperty(i => i.WallPosition, loaded.WallPosition));
                }

                if (item.Data != loaded.Data)
                    await FurniDataStore.SaveAsync(db, item.Item.Id, item.Data, loaded.Data);
            }
        }

        // everyone comes back in to the room, which loads afresh
        foreach (var entity in _entities.Entities.ToList())
        {
            SendTo(entity.Player.Id, new RoomForwardMessageComposer(_room.Id));
            await LeaveAsync(entity.Player.Id);
        }

        DeactivateOnIdle();
    }
}
