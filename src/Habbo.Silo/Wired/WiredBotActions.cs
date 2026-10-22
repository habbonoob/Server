using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>What the room grain does for wired's bot boxes: a bot saying something (talked to the room, shouted, or
/// only to some users, in a bubble width or the room's (-1)), putting on a look and keeping it, handing over a hand
/// item (from a bot, or from nobody), and walking up next to someone and then doing something.</summary>
public sealed record WiredBotActions(
    Func<RoomEntityStore.RoomEntity, string, RoomChatKind, int, IReadOnlyList<RoomEntityStore.RoomEntity>?, Task> Say,
    Func<RoomEntityStore.RoomEntity, string, Task> Dress,
    Func<RoomEntityStore.RoomEntity?, RoomEntityStore.RoomEntity, int, Task> HandOver,
    Func<RoomEntityStore.RoomEntity, RoomEntityStore.RoomEntity, Func<Task>, Task> WalkUpTo);
