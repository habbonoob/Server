using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>VoucherRedeemOkMessageComposer — the voucher's reward was handed over; furniture is named</summary>
[Message(ServerMessages.VoucherRedeemOkMessageComposer)]
public sealed partial record VoucherRedeemOkMessageComposer(string ProductDescription, string ProductName);
