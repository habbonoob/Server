namespace Habbo.Silo.Abstractions.Permissions;

/// <summary>The permissions the server asks about, by the names seed/permissions.json gives them.</summary>
public static class PermissionNames
{
    // moderation
    public const string ModerationTool = "moderation.tool";
    public const string ModerationCallForHelp = "moderation.call_for_help";
    public const string ModerationChatlog = "moderation.chatlog";
    public const string ModerationAlert = "moderation.alert";
    public const string ModerationKick = "moderation.kick";
    public const string ModerationBan = "moderation.ban";
    public const string ModerationRoomAlert = "moderation.room_alert";
    public const string ModerationRoomKick = "moderation.room_kick";

    // navigator
    public const string NavigatorStaffPick = "navigator.staff_pick";
    public const string NavigatorStaffCategories = "navigator.staff_categories";

    // the hotel and its players
    public const string HotelEnterWhileClosed = "hotel.enter_while_closed";
    public const string CalendarOpenAnyDay = "calendar.open_any_day";
    public const string UsersClassify = "users.classify";
    public const string ChatStaffStyles = "chat.staff_styles";

    // rooms
    public const string RoomEnterAny = "room.enter_any";
    public const string RoomManageAny = "room.manage_any";
    public const string RoomStaffImmunity = "room.staff_immunity";
    public const string RoomReservedTags = "room.reserved_tags";
    public const string RoomAmbassadorAlert = "room.ambassador_alert";
    public const string RoomImport = "room.import";
    public const string WiredManageAny = "wired.manage_any";
    public const string WiredGiveReward = "wired.give_reward";

    // groups and the messenger
    public const string GroupForumStaff = "group.forum_staff";
    public const string GroupForumTerminalAny = "group.forum_terminal_any";
    public const string MessengerFollowAnyone = "messenger.follow_anyone";

    /// <summary>A chat command's permission: "command." and its name.</summary>
    public static string Command(string name) => "command." + name;
}
