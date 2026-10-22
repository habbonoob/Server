using Habbo.Silo.Commands;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The room owner's commands about the room's walking, and everyone's about their own inventory.</summary>
public sealed partial class RoomGrain
{
    /// <summary>:diagonal — whether avatars here walk diagonally, switched for the room's owner.</summary>
    private async Task ToggleDiagonalAsync(RoomEntityStore.RoomEntity player)
    {
        if (!MayManage(player.Player))
            return;

        _room!.AllowDiagonal = !_room.AllowDiagonal;
        _entities!.AllowDiagonal = _room.AllowDiagonal;
        await SaveRoomAsync();
        TellPlayer(player, _room.AllowDiagonal ? "Walking diagonally is on." : "Walking diagonally is off.");
    }

    /// <summary>:empty, :emptypets, :emptybots — the player's own inventory, once they confirm.</summary>
    private async Task EmptyInventoryAsync(RoomEntityStore.RoomEntity player, string argument, InventoryPart part) =>
        TellPlayer(player, await inventoryCommands.EmptyAsync(player.Player, part, argument));
}
