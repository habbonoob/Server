using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Preferences;

/// <summary>3760 SetChatStylePreference — the chat bubble style, and since September 2026 the chat's text size.</summary>
[Incoming(ClientMessages.SetChatStylePreferenceEvent)]
public sealed partial record SetChatStylePreferenceEvent(
    int StyleId,
    [property: WireSince(ClientReleases.September2026)] int? FontSize);
