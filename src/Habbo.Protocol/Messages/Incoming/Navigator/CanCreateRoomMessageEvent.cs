using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>CanCreateRoomMessageEvent</summary>
[Incoming(ClientMessages.CanCreateRoomMessageEvent)]
public sealed partial record CanCreateRoomMessageEvent;
