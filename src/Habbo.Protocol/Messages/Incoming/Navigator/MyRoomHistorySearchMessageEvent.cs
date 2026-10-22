using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>MyRoomHistorySearchMessageEvent</summary>
[Incoming(ClientMessages.MyRoomHistorySearchMessageEvent)]
public sealed partial record MyRoomHistorySearchMessageEvent;
