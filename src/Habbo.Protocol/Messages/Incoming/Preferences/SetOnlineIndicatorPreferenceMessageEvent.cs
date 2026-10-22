using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Preferences;

/// <summary>SetOnlineIndicatorPreference (September 2026) — who sees the player online: 0 everyone, 1 the friends
/// the player has given a relationship status, 2 nobody.</summary>
[Incoming(ClientMessages.SetOnlineIndicatorPreferenceMessageEvent)]
public sealed partial record SetOnlineIndicatorPreferenceMessageEvent(int Preference);
