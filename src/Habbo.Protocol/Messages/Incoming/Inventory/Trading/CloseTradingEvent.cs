using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Trading;

/// <summary>CloseTradingEvent</summary>
[Incoming(ClientMessages.CloseTradingEvent)]
public sealed partial record CloseTradingEvent;
