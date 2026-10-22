using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Trading;

/// <summary>AcceptTradingEvent</summary>
[Incoming(ClientMessages.AcceptTradingEvent)]
public sealed partial record AcceptTradingEvent;
