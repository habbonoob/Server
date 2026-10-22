using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Abstractions.Inventory;

/// <summary>One inventory item as the FurniList packet needs it.</summary>
[GenerateSerializer]
public partial record InventoryItemData(
    [property: Id(0)] int ItemId,
    [property: Id(1)] int ClassId,
    [property: Id(2)] bool IsWallItem,
    [property: Id(3)] FurniDataView Data,
    [property: Id(4)] int RoomId,
    [property: Id(5)] int Category,
    [property: Id(6)] bool Recyclable,
    [property: Id(7)] bool Tradeable,
    [property: Id(8)] bool Groupable,
    [property: Id(9)] bool Sellable,
    [property: Id(10)] int SecondsToExpiration,
    [property: Id(11)] bool HasRentPeriodStarted,
    [property: Id(12)] string SlotId,
    [property: Id(13)] int Extra);
