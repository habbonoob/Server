using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Habbicons;

/// <summary>TriggerHabbiconMessageEvent (September 2026) — show an owned Habbicon over the avatar, to the room.</summary>
[Incoming(ClientMessages.TriggerHabbiconMessageEvent)]
public sealed partial record TriggerHabbiconMessageEvent(int HabbiconId);
