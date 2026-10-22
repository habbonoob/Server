using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>WiredTransactionGetChestLogsMessageEvent — a chest's transactions, a page at a time</summary>
[Incoming(ClientMessages.WiredTransactionGetChestLogsMessageEvent)]
public sealed partial record WiredTransactionGetChestLogsMessageEvent(int ChestId, int PageSize, int Page);
