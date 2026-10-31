using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Habbicons;

/// <summary>BuyHabbiconMessageEvent (September 2026) — buy a Habbicon from its collection in the Habbicons Collection.</summary>
[Incoming(ClientMessages.BuyHabbiconMessageEvent)]
public sealed partial record BuyHabbiconMessageEvent(int HabbiconId);
