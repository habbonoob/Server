using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Habbicons;

/// <summary>GetHabbiconShopDataMessageEvent (September 2026) — every collection and its Habbicons, as the player stands with them.</summary>
[Incoming(ClientMessages.GetHabbiconShopDataMessageEvent)]
public sealed partial record GetHabbiconShopDataMessageEvent;
