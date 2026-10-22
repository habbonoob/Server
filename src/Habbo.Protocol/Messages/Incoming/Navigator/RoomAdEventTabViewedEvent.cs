using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>RoomAdEventTabViewedEvent</summary>
[Incoming(ClientMessages.RoomAdEventTabViewedEvent)]
public sealed partial record RoomAdEventTabViewedEvent;
