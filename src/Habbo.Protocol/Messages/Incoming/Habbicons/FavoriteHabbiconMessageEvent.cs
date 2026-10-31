using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Habbicons;

/// <summary>FavoriteHabbiconMessageEvent (September 2026) — star an owned Habbicon, listing it under the favourites tab.</summary>
[Incoming(ClientMessages.FavoriteHabbiconMessageEvent)]
public sealed partial record FavoriteHabbiconMessageEvent(int HabbiconId);
