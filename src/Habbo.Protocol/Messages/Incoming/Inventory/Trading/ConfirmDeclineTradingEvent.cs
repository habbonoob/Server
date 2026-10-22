using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Trading;

/// <summary>ConfirmDeclineTradingEvent</summary>
[Incoming(ClientMessages.ConfirmDeclineTradingEvent)]
public sealed partial record ConfirmDeclineTradingEvent;
