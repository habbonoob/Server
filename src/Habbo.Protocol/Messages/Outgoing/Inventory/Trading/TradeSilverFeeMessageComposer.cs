using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Trading;

/// <summary>TradeSilverFeeMessageComposer</summary>
[Message(ServerMessages.TradeSilverFeeMessageComposer)]
public sealed partial record TradeSilverFeeMessageComposer(int SilverFee);
