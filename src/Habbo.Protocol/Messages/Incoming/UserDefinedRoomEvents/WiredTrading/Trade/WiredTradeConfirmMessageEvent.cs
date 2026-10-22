using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Trade;

/// <summary>WiredTradeConfirmMessageEvent — accept a wired trade, then confirm it</summary>
[Incoming(ClientMessages.WiredTradeConfirmMessageEvent)]
public sealed partial record WiredTradeConfirmMessageEvent(bool Confirmed);
