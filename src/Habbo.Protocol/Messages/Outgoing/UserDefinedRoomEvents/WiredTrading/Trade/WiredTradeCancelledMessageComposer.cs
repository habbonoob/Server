using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade;

/// <summary>WiredTradeCancelledMessageComposer — a wired trade was called off</summary>
[Message(ServerMessages.WiredTradeCancelledMessageComposer)]
public sealed partial record WiredTradeCancelledMessageComposer(int TransactionFailureTypeId);
