using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>WiredTransactionGetLogDetailsMessageEvent — one transaction in full</summary>
[Incoming(ClientMessages.WiredTransactionGetLogDetailsMessageEvent)]
public sealed partial record WiredTransactionGetLogDetailsMessageEvent(long TransactionId);
