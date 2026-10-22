using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>VoucherRedeemErrorMessageComposer — catalog.alert.voucherredeem.error.description.N</summary>
[Message(ServerMessages.VoucherRedeemErrorMessageComposer)]
public sealed partial record VoucherRedeemErrorMessageComposer(string ErrorCode)
{
    public const string Invalid = "0";
    public const string Technical = "1";
}
