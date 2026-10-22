using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>WiredTransactionFailMessageComposer — a wired transaction failed</summary>
[Message(ServerMessages.WiredTransactionFailMessageComposer)]
public sealed partial record WiredTransactionFailMessageComposer(int TransactionFailureTypeId);
