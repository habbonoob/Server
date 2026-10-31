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

    /// <summary>What the room knows of a pet, for the ~pet and ~plant variables; null when it is no pet of the room's.</summary>
    public Func<RoomEntityStore.RoomEntity, WiredPetStats?> PetStats { get; init; } = _ => null;

    /// <summary>The user's wired trade (a deposit, or a payment or trade under a contract), if they are in one.</summary>
    public Func<RoomEntityStore.RoomEntity, WiredTrade?> TradeOf { get; init; } = _ => null;

    /// <summary>Where a teleport leads now (its partner, or where wired pointed it); and pointing it elsewhere.</summary>
    public Func<RoomItemStore.PlacedItem, int?> TeleportTarget { get; init; } = _ => null;
    public Func<RoomItemStore.PlacedItem, int, Task<bool>> SetTeleportTarget { get; init; } = (_, _) => Task.FromResult(false);

    /// <summary>Hangs a wall item somewhere else on the walls, saved and shown; false where it may not hang.</summary>
    public Func<RoomItemStore.PlacedItem, WallLocation, Task<bool>> MoveWallItem { get; init; } = (_, _) => Task.FromResult(false);

    /// <summary>Shows a Habbicon over the user's head, for everyone.</summary>
    public Func<RoomEntityStore.RoomEntity, int, Task> ShowHabbicon { get; init; } = (_, _) => Task.CompletedTask;

    /// <summary>Sends a payload through a WIRED Link: back to the Link itself when it receives its own room's, and over its
    /// network to the other rooms' Links.</summary>
    public Func<RoomItemStore.PlacedItem, WiredLinkPayload, Task> SendLink { get; init; } = (_, _) => Task.CompletedTask;
}
