using Habbo.Domain.Users;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Protocol.Messages.Outgoing.Preferences;

/// <summary>2809 AccountPreferencesComposer. September 2026 reads the free-flow flag without using it, and after
/// the rest the chat settings that were the room's, and who sees the player online.</summary>
[Message(ServerMessages.AccountPreferencesComposer)]
public sealed partial record AccountPreferencesComposer(
    int UiVolume,
    int FurniVolume,
    int TraxVolume,
    bool FreeFlowChatDisabled,
    bool RoomInvitesIgnored,
    bool RoomCameraFollowDisabled,
    int UiFlags,
    int PreferedChatStyle,
    bool WiredMenuButton,
    bool WiredInspectButton,
    bool PlayTestMode,
    int Unused,
    bool WiredWhisperDisabled,
    bool ShowAllNotifications,
    string WiredUiStyle,
    [property: WireSince(ClientReleases.September2026)] int ChatFontSize,
    [property: WireSince(ClientReleases.September2026)] int ChatMode,
    [property: WireSince(ClientReleases.September2026)] int ChatBubbleWidth,
    [property: WireSince(ClientReleases.September2026)] int ChatScrollSpeed,
    [property: WireSince(ClientReleases.September2026)] OnlineIndicatorPreference OnlineIndicator)
{
    /// <summary>The parser reads and discards one int between playTestMode and wiredWhisperDisabled.</summary>
    private const int UnusedPreferenceSlot = 0;

    public static AccountPreferencesComposer From(AccountPreferences preferences) => new(
        preferences.UiVolume,
        preferences.FurniVolume,
        preferences.TraxVolume,
        preferences.FreeFlowChatDisabled,
        preferences.RoomInvitesIgnored,
        preferences.RoomCameraFollowDisabled,
        preferences.UiFlags,
        preferences.PreferedChatStyle,
        preferences.WiredMenuButton,
        preferences.WiredInspectButton,
        preferences.PlayTestMode,
        UnusedPreferenceSlot,
        preferences.WiredWhisperDisabled,
        preferences.ShowAllNotifications,
        preferences.WiredUiStyle,
        preferences.ChatFontSize,
        preferences.ChatMode,
        preferences.ChatBubbleWidth,
        preferences.ChatScrollSpeed,
        preferences.OnlineIndicator);
}
