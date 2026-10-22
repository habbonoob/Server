using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>3825 WiredSetPreferences — the player's wired menu preferences. The int between the
/// play-test flag and the whisper flag is always 0; AccountPreferences skips it the same way.</summary>
[Incoming(ClientMessages.WiredSetPreferencesMessageEvent)]
public sealed partial record WiredSetPreferencesMessageEvent(
    bool WiredMenuButton,
    bool WiredInspectButton,
    bool PlayTestMode,
    int Unused,
    bool WiredWhisperDisabled,
    bool ShowAllNotifications,
    string UiStyle);
