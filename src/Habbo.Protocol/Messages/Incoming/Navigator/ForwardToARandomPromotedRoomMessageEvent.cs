using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>ForwardToARandomPromotedRoomMessageEvent</summary>
[Incoming(ClientMessages.ForwardToARandomPromotedRoomMessageEvent)]
public sealed partial record ForwardToARandomPromotedRoomMessageEvent(string CategoryCode);
