using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Trade;

/// <summary>WiredTradeAddDeleteItemsMessageEvent — put items into a wired trade, or take them out</summary>
[Incoming(ClientMessages.WiredTradeAddDeleteItemsMessageEvent)]
public sealed partial record WiredTradeAddDeleteItemsMessageEvent(bool Remove, IReadOnlyList<int> ItemIds);
