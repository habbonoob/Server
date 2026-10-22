using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>WiredTransactionLogListMessageComposer — a page of a chest's or a room's transactions</summary>
[Message(ServerMessages.WiredTransactionLogListMessageComposer)]
public sealed partial record WiredTransactionLogListMessageComposer(int LogListType, long LogListId, int TotalLogs, int CurrentPage, int Amount, IReadOnlyList<WiredTransactionInfoData> Logs)
{
    /// <summary>Whose transactions the list holds.</summary>
    public const int ChestLogs = 0;
    public const int RoomLogs = 1;
}
