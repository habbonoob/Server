using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Trading;

/// <summary>TradingAcceptComposer</summary>
[Message(ServerMessages.TradingAcceptComposer)]
public sealed partial record TradingAcceptComposer(int UserId, int UserAccepts);
