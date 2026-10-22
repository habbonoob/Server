using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>RoomSettingsDataComposer. September 2026 dropped the navigator's dynamic categories flag and reads how the
/// room treats its visitors in its place.</summary>
[Message(ServerMessages.RoomSettingsDataComposer)]
public sealed partial record RoomSettingsDataComposer(
    int RoomId,
    string Name,
    string Description,
    int DoorMode,
    int CategoryId,
    int MaximumVisitors,
    int MaximumVisitorsLimit,
    IReadOnlyList<string> Tags,
    int TradeMode,
    int AllowPets,
    int AllowFoodConsume,
    int AllowWalkThrough,
    int HideWalls,
    int WallThickness,
    int FloorThickness,
    RoomChatSettingsData ChatSettings,
    [property: WireUntil(ClientReleases.September2026)] bool AllowNavigatorDynamicCategories,
    [property: WireSince(ClientReleases.September2026)] bool LeaveOnDoorTile,
    [property: WireSince(ClientReleases.September2026)] bool IdleSleepEnabled,
    [property: WireSince(ClientReleases.September2026)] int IdleSleepTimeoutSeconds,
    [property: WireSince(ClientReleases.September2026)] bool IdleAutokickEnabled,
    [property: WireSince(ClientReleases.September2026)] int IdleAutokickTimeoutSeconds,
    [property: WireSince(ClientReleases.September2026)] bool MuteAllPets,
    RoomModerationSettingsData ModerationSettings,
    bool HiddenByBuildersClub);
