using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Trading;

/// <summary>TradingCloseComposer — the trade closed, by whom; reason 1 is a failed commit</summary>
[Message(ServerMessages.TradingCloseComposer)]
public sealed partial record TradingCloseComposer(int UserId, int Reason)
{
    public const int Closed = 0;
    public const int CommitFailed = 1;
}
