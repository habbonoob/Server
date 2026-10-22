using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade;

/// <summary>WiredTradeCompletedMessageComposer — a wired trade went through</summary>
[Message(ServerMessages.WiredTradeCompletedMessageComposer)]
public sealed partial record WiredTradeCompletedMessageComposer;
