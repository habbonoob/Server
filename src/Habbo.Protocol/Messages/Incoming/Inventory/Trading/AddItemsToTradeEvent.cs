using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Trading;

/// <summary>AddItemsToTradeEvent</summary>
[Incoming(ClientMessages.AddItemsToTradeEvent)]
public sealed partial record AddItemsToTradeEvent(IReadOnlyList<int> ItemIds);
