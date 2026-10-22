using Habbo.Domain.Rooms;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Silo.Rooms;

/// <summary>Projects a stored room into the navigator/GetGuestRoom shape. The single place
/// that decides requester-specific fields.</summary>
public static class RoomProjection
{
    public static RoomInfoData ToInfo(
        Room room, int userCount, Guid requesterId, RoomGroup? group, bool isGroupMember, RoomAdSummary? ad = null) => new(
        room.Id,
        room.Name,
        room.Owner?.AccountId ?? 0,
        room.Owner?.Username ?? string.Empty,
        ToDoorMode(room.State),
        userCount,
        room.MaxUsers,
        room.Description,
        room.IsStaffPick,
        isGroupMember,
        room.AllInRoomMuted,
        room.WhoCanMute,
        room.WhoCanKick,
        room.WhoCanBan,
        CanMute: requesterId == room.OwnerId,
        room.ChatMode,
        room.ChatBubbleWidth,
        room.ChatScrollSpeed,
        room.ChatHearRange,
        room.ChatFloodSensitivity,
        room.TradeMode,
        room.Score,
        room.Ranking,
        room.CategoryId,
        room.Tags,
        room.OfficialPicRef ?? string.Empty,
        room.ShowOwner,
        room.AllowPets,
        group,
        ad);

    /// <summary>Wire door modes: 0 open, 1 doorbell, 2 password, 3 invisible, 4 noob room.</summary>
    private static int ToDoorMode(RoomState state) => state switch
    {
        RoomState.Open => 0,
        RoomState.Locked => 1,
        RoomState.Password => 2,
        RoomState.Invisible => 3,
        RoomState.NoobsOnly => 4,
        _ => 0,
    };
}
