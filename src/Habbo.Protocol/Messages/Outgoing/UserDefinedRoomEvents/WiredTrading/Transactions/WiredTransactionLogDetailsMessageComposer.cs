using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>WiredTransactionLogDetailsMessageComposer — one transaction in full</summary>
[Message(ServerMessages.WiredTransactionLogDetailsMessageComposer)]
public sealed partial record WiredTransactionLogDetailsMessageComposer(WiredTransactionInfoData Info, IReadOnlyList<int> ChestIds, IReadOnlyList<ChestItemAmountData> Deposited, IReadOnlyList<ChestItemAmountData> Withdrawn, bool IsIncompleteData);
