using Habbo.Protocol.Messages.Outgoing.Inventory.Trading;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade;

/// <summary>WiredTradeItemsUpdateMessageComposer — what each side of a wired trade offers</summary>
[Message(ServerMessages.WiredTradeItemsUpdateMessageComposer)]
public sealed partial record WiredTradeItemsUpdateMessageComposer(int FirstUserId, IReadOnlyList<TradeItemData> FirstUserItems, int FirstUserNumItems, int FirstUserNumCredits, int SecondUserId, IReadOnlyList<TradeItemData> SecondUserItems, int SecondUserNumItems, int SecondUserNumCredits, bool CanAccept, int Extra);
