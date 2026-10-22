using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>Everything the gateway needs to push the room-entry sequence.</summary>
[GenerateSerializer]
public partial record RoomLoadData(
    [property: Id(0)] int RoomId,
    [property: Id(1)] string RoomName,
    [property: Id(2)] int OwnerAccountId,
    [property: Id(3)] string OwnerName,
    [property: Id(4)] bool IsOwner,
    [property: Id(5)] string ModelName,
    [property: Id(6)] string Heightmap,
    [property: Id(7)] int Width,
    [property: Id(8)] int Height,
    [property: Id(9)] int DoorX,
    [property: Id(10)] int DoorY,
    [property: Id(11)] int DoorDirection,
    [property: Id(12)] List<RoomEntityData> Entities,
    [property: Id(13)] int OwnRoomIndex,
    [property: Id(14)] RoomVisualizationData Visualization,
    [property: Id(15)] List<HeightMapTileData> Tiles,
    [property: Id(16)] List<RoomItemData> FloorItems,
    [property: Id(17)] List<RoomItemData> WallItems,
    [property: Id(18)] int RoomControllerLevel,
    [property: Id(19)] List<AvatarUpdateData> Statuses,
    [property: Id(20)] List<AreaHideData> AreaHides,
    /// <summary>The room's likes, and whether the entrant may still like it.</summary>
    [property: Id(21)] int Score = 0,
    [property: Id(22)] bool CanRate = false,
    /// <summary>The room's running promotion, if any.</summary>
    [property: Id(23)] RoomEventInfo? Event = null,
    /// <summary>What the room's configuration furniture switches on.</summary>
    [property: Id(24)] RoomConfigurationStates? Configuration = null,
    /// <summary>Whether the room's wired listens for clicks on users.</summary>
    [property: Id(25)] bool WiredListensForUserClicks = false,
    /// <summary>Whether the entrant may change, and read, the room's wired.</summary>
    [property: Id(26)] bool CanModifyWired = false,
    [property: Id(27)] bool CanReadWired = false);
