using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Trading;

/// <summary>AddItemToTradeEvent</summary>
[Incoming(ClientMessages.AddItemToTradeEvent)]
public sealed partial record AddItemToTradeEvent(int ItemId);
