using Habbo.Protocol.Composers;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Items.Interactions;

/// <summary>What a behaviour may do to the room its item stands in.</summary>
public interface IFurniRoom
{
    /// <summary>Stores the item's new data and shows it to everyone in the room.</summary>
    Task UpdateAsync(RoomItemStore.PlacedItem item, FurniDataView data);

    /// <summary>The same, kept in the room only when <paramref name="persist"/> is false: for data that changes every
    /// second (a running clock) and is stored once it settles.</summary>
    Task UpdateAsync(RoomItemStore.PlacedItem item, FurniDataView data, bool persist) => UpdateAsync(item, data);

    /// <summary>Shows everyone the item's data as it now stands.</summary>
    void ShowData(RoomItemStore.PlacedItem item);

    /// <summary>Runs work that may change many items' data and shows what changed all at once.</summary>
    Task BatchedAsync(Func<Task> work);

    /// <summary>Stores the item's new data without announcing it, for a behaviour that tells the
    /// room in a message of its own.</summary>
    Task SaveAsync(RoomItemStore.PlacedItem item, FurniDataView data);

    /// <summary>Takes the item out of the room and out of the database, for a behaviour whose
    /// item is consumed rather than changed. It does not come back to anyone's inventory. False when it was
    /// already gone from the database (redeemed or taken elsewhere) or was only a wired copy, so nothing that pays for it should.</summary>
    Task<bool> RemoveAsync(RoomItemStore.PlacedItem item);

    /// <summary>Pays the player, and tells them their new balance.</summary>
    Task GiveCreditsAsync(RoomEntityStore.RoomEntity user, int credits);

    void Broadcast(IComposer composer);

    /// <summary>Runs <paramref name="action"/> later, on the room's own turn.</summary>
    void Schedule(TimeSpan delay, Func<Task> action);

    /// <summary>Whether the item is still where it was: a scheduled step checks this, because
    /// the item may have been picked up in the meantime.</summary>
    bool IsPlaced(RoomItemStore.PlacedItem item);

    /// <summary>Whether an avatar stands on the item.</summary>
    bool IsOccupied(RoomItemStore.PlacedItem item);

    /// <summary>Sends one avatar's player a message.</summary>
    void Tell(RoomEntityStore.RoomEntity user, IComposer composer);

    /// <summary>Whether the avatar stands on or next to the item — close enough to touch it.</summary>
    bool IsWithinReach(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item);

    /// <summary>Walks the avatar to a tile and then does something, unless the player walks off
    /// somewhere else first. Already there, it happens at once.</summary>
    Task WalkThenAsync(RoomEntityStore.RoomEntity user, int x, int y, Func<Task> then);

    /// <summary>Turns the avatar to face a tile, for everyone to see.</summary>
    void LookAt(RoomEntityStore.RoomEntity user, int x, int y);

    /// <summary>Dresses the avatar in an outfit for this room only, or takes it off (null).</summary>
    void DressInRoom(RoomEntityStore.RoomEntity user, string? outfitFigure);

    /// <summary>Puts something in the avatar's hand (0 empties it), for everyone to see.</summary>
    void SetHandItem(RoomEntityStore.RoomEntity user, int handItem);

    /// <summary>Dresses the avatar in an effect for this room only, for everyone to see.</summary>
    void WearEffect(RoomEntityStore.RoomEntity user, int effectId);
}
