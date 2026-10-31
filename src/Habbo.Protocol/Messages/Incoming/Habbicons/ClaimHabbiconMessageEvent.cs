using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Habbicons;

/// <summary>ClaimHabbiconMessageEvent (September 2026) — claim a completed collection's reward Habbicon.</summary>
[Incoming(ClientMessages.ClaimHabbiconMessageEvent)]
public sealed partial record ClaimHabbiconMessageEvent(int HabbiconId);
