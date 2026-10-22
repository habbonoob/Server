using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>A room row for navigator lists and the GetGuestRoomResult round-trip.</summary>
[GenerateSerializer]
public partial record RoomInfoData(
    [property: Id(0)] int RoomId,
    [property: Id(1)] string Name,
    [property: Id(2)] int OwnerAccountId,
    [property: Id(3)] string OwnerName,
    [property: Id(4)] int DoorMode,
    [property: Id(5)] int UserCount,
    [property: Id(6)] int MaxUsers,
    [property: Id(7)] string Description,
    [property: Id(8)] bool IsStaffPick,
    [property: Id(9)] bool IsGroupMember,
    [property: Id(10)] bool AllInRoomMuted,
    [property: Id(11)] int WhoCanMute,
    [property: Id(12)] int WhoCanKick,
    [property: Id(13)] int WhoCanBan,
    [property: Id(14)] bool CanMute,
    [property: Id(15)] int ChatMode,
    [property: Id(16)] int ChatBubbleWidth,
    [property: Id(17)] int ChatScrollSpeed,
    [property: Id(18)] int ChatHearRange,
    [property: Id(19)] int ChatFloodSensitivity,
    [property: Id(20)] int TradeMode,
    [property: Id(21)] int Score,
    [property: Id(22)] int Ranking,
    [property: Id(23)] int CategoryId,
    [property: Id(24)] List<string> Tags,
    [property: Id(25)] string OfficialPicRef,
    [property: Id(26)] bool ShowOwner,
    [property: Id(27)] bool AllowPets,
    /// <summary>The group the room is home to, if any.</summary>
    [property: Id(28)] RoomGroup? Group,
    /// <summary>The room's running promotion, if any.</summary>
    [property: Id(29)] RoomAdSummary? Ad = null);
