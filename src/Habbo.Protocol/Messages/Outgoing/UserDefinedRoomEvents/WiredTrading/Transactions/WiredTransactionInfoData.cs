using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>WiredTransactionInfoData — a transaction in a log</summary>
[WireStruct]
public sealed partial record WiredTransactionInfoData(long TransactionId, int FlatId, int TransactionType, string TransactionDefinitionInfo, int UserId, string UserName, long Timestamp, string ReadableTimestamp, int ChestCount, int WithdrawFurniCount, int DepositFurniCount, int WithdrawCoinsCount, int DepositCoinsCount);
