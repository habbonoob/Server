using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Preferences;

/// <summary>1407 SetChatPreferences — whether the old chat style is preferred. September 2026 always sends false
/// there, then the player's chat mode, bubble width and scroll speed.</summary>
[Incoming(ClientMessages.SetChatPreferencesMessageEvent)]
public sealed partial record SetChatPreferencesMessageEvent(
    bool PreferOldChat,
    [property: WireSince(ClientReleases.September2026)] int? ChatMode,
    [property: WireSince(ClientReleases.September2026)] int? ChatBubbleWidth,
    [property: WireSince(ClientReleases.September2026)] int? ChatScrollSpeed);
