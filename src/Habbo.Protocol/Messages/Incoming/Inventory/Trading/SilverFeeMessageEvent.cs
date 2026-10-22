using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Trading;

/// <summary>SilverFeeMessageEvent — offer to pay the trade's silver fee</summary>
[Incoming(ClientMessages.SilverFeeMessageEvent)]
public sealed partial record SilverFeeMessageEvent(bool PaySilverFee);
