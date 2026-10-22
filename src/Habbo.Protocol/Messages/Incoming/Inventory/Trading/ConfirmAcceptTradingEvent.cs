using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Trading;

/// <summary>ConfirmAcceptTradingEvent</summary>
[Incoming(ClientMessages.ConfirmAcceptTradingEvent)]
public sealed partial record ConfirmAcceptTradingEvent;
