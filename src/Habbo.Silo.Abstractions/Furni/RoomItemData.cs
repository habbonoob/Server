using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Furni;

/// <summary>A room item as the gateway needs it for Objects/Items/ObjectAdd packets.</summary>
[GenerateSerializer]
public partial record RoomItemData(
    [property: Id(0)] int ItemId,
    [property: Id(1)] int ClassId,
    [property: Id(2)] bool IsWallItem,
    [property: Id(3)] int X,
    [property: Id(4)] int Y,
    [property: Id(5)] string Z,
    [property: Id(6)] int Rotation,
    [property: Id(7)] string SizeZ,
    [property: Id(8)] FurniDataView Data,
    [property: Id(9)] string WallPosition,
    [property: Id(10)] int OwnerAccountId,
    [property: Id(11)] string OwnerName,
    [property: Id(12)] int Extra,
    [property: Id(13)] int ExpiryTime,
    [property: Id(14)] int UsagePolicy);
