using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Trading;

/// <summary>TradingOpenComposer — the trade opened; each side's account id and whether they may give</summary>
[Message(ServerMessages.TradingOpenComposer)]
public sealed partial record TradingOpenComposer(int UserId, int UserCanTrade, int OtherUserId, int OtherUserCanTrade);
