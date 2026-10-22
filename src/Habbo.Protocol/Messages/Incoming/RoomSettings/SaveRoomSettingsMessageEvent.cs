using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.RoomSettings;

/// <summary>SaveRoomSettingsMessageEvent. September 2026 sends no chat mode, bubble size, scroll speed or hearing range
/// (the first three are each player's own there) and no dynamic categories flag, and sends how the room treats its
/// visitors after the flood sensitivity; what a release does not send is not changed.</summary>
[Incoming(ClientMessages.SaveRoomSettingsMessageEvent)]
public sealed partial record SaveRoomSettingsMessageEvent(
    int RoomId,
    string Name,
    string Description,
    int DoorMode,
    string Password,
    int MaximumVisitors,
    int CategoryId,
    IReadOnlyList<string> Tags,
    int TradeMode,
    bool AllowPets,
    bool AllowFoodConsume,
    bool AllowWalkThrough,
    bool HideWalls,
    int WallThickness,
    int FloorThickness,
    int WhoCanMute,
    int WhoCanKick,
    int WhoCanBan,
    [property: WireUntil(ClientReleases.September2026)] int? ChatMode,
    [property: WireUntil(ClientReleases.September2026)] int? ChatBubbleSize,
    [property: WireUntil(ClientReleases.September2026)] int? ChatScrollUpFrequency,
    [property: WireUntil(ClientReleases.September2026)] int? ChatFullHearRange,
    int ChatFloodSensitivity,
    [property: WireUntil(ClientReleases.September2026)] bool? AllowNavigatorDynamicCategories,
    [property: WireSince(ClientReleases.September2026)] bool? LeaveOnDoorTile,
    [property: WireSince(ClientReleases.September2026)] bool? IdleSleepEnabled,
    [property: WireSince(ClientReleases.September2026)] int? IdleSleepTimeoutSeconds,
    [property: WireSince(ClientReleases.September2026)] bool? IdleAutokickEnabled,
    [property: WireSince(ClientReleases.September2026)] int? IdleAutokickTimeoutSeconds,
    [property: WireSince(ClientReleases.September2026)] bool? MuteAllPets);
