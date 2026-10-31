using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Habbicons;

/// <summary>UnfavoriteHabbiconMessageEvent (September 2026) — take the star off a Habbicon.</summary>
[Incoming(ClientMessages.UnfavoriteHabbiconMessageEvent)]
public sealed partial record UnfavoriteHabbiconMessageEvent(int HabbiconId);
