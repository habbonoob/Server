using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Abstractions.Wired;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>What the room grain lets its wired do that belongs to the grain: move furniture (with
/// everything a move tells the room), dress avatars in effects, send users out, read the game
/// texts, and write to the room's wired log (level, source, message).</summary>
public sealed record WiredRoomActions(
    Func<RoomItemStore.PlacedItem, int, int, int, WiredMoveOptions, Task<bool>> MoveItem,
    Func<RoomItemStore.PlacedItem, double, Task<bool>> SetAltitude,
    Action<RoomEntityStore.RoomEntity, int> ApplyEffect,
    Func<RoomEntityStore.RoomEntity, Task> Kick,
    IGameTexts Texts,
    Func<byte, byte, string, Task> Log,
    Func<RoomEntityStore.RoomEntity, int, Task> Dance,
    Func<RoomEntityStore.RoomEntity, int, Task> Sign,
    Func<SharedVariableChange, Task> SharedChanged,
    IAvatarEffectCatalog Effects,
    IWiredChests Chests,
    Rewards.IWiredRewards Rewards)
{
    /// <summary>What the room does for the bot boxes.</summary>
    public required WiredBotActions Bots { get; init; }

    /// <summary>Sends a user straight through a teleport, out of its partner.</summary>
    public required Func<RoomEntityStore.RoomEntity, RoomItemStore.PlacedItem, Task> SendThrough { get; init; }
}
