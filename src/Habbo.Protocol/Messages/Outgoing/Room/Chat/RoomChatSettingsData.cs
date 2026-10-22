using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>RoomChatSettingsData — a room's chat settings. Since September 2026 the client only reads the flood
/// sensitivity: the mode, bubble width and scroll speed became each player's own (AccountPreferences), and the
/// hearing range is the server's to apply.</summary>
[WireStruct]
public sealed partial record RoomChatSettingsData(
    [property: WireUntil(ClientReleases.September2026)] int Mode,
    [property: WireUntil(ClientReleases.September2026)] int BubbleWidth,
    [property: WireUntil(ClientReleases.September2026)] int ScrollSpeed,
    [property: WireUntil(ClientReleases.September2026)] int FullHearRange,
    int FloodSensitivity);
