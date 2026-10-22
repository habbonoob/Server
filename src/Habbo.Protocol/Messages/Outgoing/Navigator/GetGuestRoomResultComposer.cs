using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>3576 GetGuestRoomResultComposer — the room struct, then the moderation and chat
/// settings the room UI needs.</summary>
[Message(ServerMessages.GetGuestRoomResultComposer)]
public sealed partial record GetGuestRoomResultComposer(
    bool EnterRoom,
    GuestRoomData Room,
    bool RoomForward,
    bool IsStaffPick,
    bool IsGroupMember,
    bool AllInRoomMuted,
    int WhoCanMute,
    int WhoCanKick,
    int WhoCanBan,
    bool CanMute,
    RoomChatSettingsData ChatSettings,
    bool OpeningConnection)
{
    public static GetGuestRoomResultComposer From(
        bool enterRoom, RoomInfoData room, bool roomForward, bool openingConnection) => new(
        enterRoom,
        GuestRoomData.From(room),
        roomForward,
        room.IsStaffPick,
        room.IsGroupMember,
        room.AllInRoomMuted,
        room.WhoCanMute,
        room.WhoCanKick,
        room.WhoCanBan,
        room.CanMute,
        new RoomChatSettingsData(room.ChatMode, room.ChatBubbleWidth, room.ChatScrollSpeed, room.ChatHearRange, room.ChatFloodSensitivity),
        openingConnection);
}
