using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Trading;

/// <summary>TradingItemListComposer — what each side offers, and its credit furni's worth</summary>
[Message(ServerMessages.TradingItemListComposer)]
public sealed partial record TradingItemListComposer(int FirstUserId, IReadOnlyList<TradeItemData> FirstUserItems, int FirstUserNumItems, int FirstUserNumCredits, int SecondUserId, IReadOnlyList<TradeItemData> SecondUserItems, int SecondUserNumItems, int SecondUserNumCredits);
