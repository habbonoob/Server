using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>RoomTextSearchMessageEvent</summary>
[Incoming(ClientMessages.RoomTextSearchMessageEvent)]
public sealed partial record RoomTextSearchMessageEvent(string SearchText);
