using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Trade;

/// <summary>WiredTradeCancelMessageEvent — call off a wired trade</summary>
[Incoming(ClientMessages.WiredTradeCancelMessageEvent)]
public sealed partial record WiredTradeCancelMessageEvent;
