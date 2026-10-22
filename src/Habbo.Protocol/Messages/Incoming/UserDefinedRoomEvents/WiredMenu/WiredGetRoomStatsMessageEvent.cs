using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>2934 WiredGetRoomStats — the wired menu's monitor asks for the room's statistics.</summary>
[Incoming(ClientMessages.WiredGetRoomStatsMessageEvent)]
public sealed partial record WiredGetRoomStatsMessageEvent;
