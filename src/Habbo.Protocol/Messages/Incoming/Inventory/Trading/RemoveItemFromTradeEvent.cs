using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Trading;

/// <summary>RemoveItemFromTradeEvent</summary>
[Incoming(ClientMessages.RemoveItemFromTradeEvent)]
public sealed partial record RemoveItemFromTradeEvent(int ItemId);
