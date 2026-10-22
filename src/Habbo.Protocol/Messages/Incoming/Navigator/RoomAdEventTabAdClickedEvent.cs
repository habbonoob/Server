using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>RoomAdEventTabAdClickedEvent</summary>
[Incoming(ClientMessages.RoomAdEventTabAdClickedEvent)]
public sealed partial record RoomAdEventTabAdClickedEvent(int FlatId, string RoomAdName, int RoomAdExpiresInMinutes);
