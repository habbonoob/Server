using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>WiredTransactionGetRoomLogsMessageEvent — the room's chest transactions, a page at a time</summary>
[Incoming(ClientMessages.WiredTransactionGetRoomLogsMessageEvent)]
public sealed partial record WiredTransactionGetRoomLogsMessageEvent(int PageSize, int Page);
