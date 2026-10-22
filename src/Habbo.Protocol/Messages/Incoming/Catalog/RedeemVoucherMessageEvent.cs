using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>RedeemVoucherMessageEvent</summary>
[Incoming(ClientMessages.RedeemVoucherMessageEvent)]
public sealed partial record RedeemVoucherMessageEvent(string Code);
