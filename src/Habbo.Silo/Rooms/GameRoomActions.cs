namespace Habbo.Silo.Rooms;

/// <summary>What the room's furni games do to the room beyond furniture data, which the grain owns: moving an item
/// (x, y, rotation, and whether it may go onto furniture: a football into a goal; the Banzai puck, a football), putting an avatar on a tile (a Freeze player out of lives, to the exit), freezing one where it
/// stands, and counting achievements (a player's, and the room owner's).</summary>
public sealed record GameRoomActions(
    Func<RoomItemStore.PlacedItem, int, int, int, bool, Task<bool>> MoveItem,
    Func<int, int, bool> HasAvatarAt,
    Action<RoomEntityStore.RoomEntity, int, int> Warp,
    Action<RoomEntityStore.RoomEntity, bool> SetFrozen,
    Action<RoomEntityStore.RoomEntity, string, int> ProgressAchievement,
    Action<string, int>? ProgressOwnerAchievement = null);
