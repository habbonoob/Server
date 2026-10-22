using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Trading;

/// <summary>TradingConfirmationComposer — both accepted: each now confirms</summary>
[Message(ServerMessages.TradingConfirmationComposer)]
public sealed partial record TradingConfirmationComposer;
