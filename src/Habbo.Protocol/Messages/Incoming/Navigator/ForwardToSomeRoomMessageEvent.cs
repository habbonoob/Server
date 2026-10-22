using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>ForwardToSomeRoomMessageEvent</summary>
[Incoming(ClientMessages.ForwardToSomeRoomMessageEvent)]
public sealed partial record ForwardToSomeRoomMessageEvent(string Type);
