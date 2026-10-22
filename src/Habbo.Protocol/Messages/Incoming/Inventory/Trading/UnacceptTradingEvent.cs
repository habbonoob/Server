using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Trading;

/// <summary>UnacceptTradingEvent</summary>
[Incoming(ClientMessages.UnacceptTradingEvent)]
public sealed partial record UnacceptTradingEvent;
