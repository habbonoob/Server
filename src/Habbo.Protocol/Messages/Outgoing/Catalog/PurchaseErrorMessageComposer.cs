using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>2430 PurchaseError — the purchase was refused. The client turns a positive code
/// into the external text "catalog.alert.purchaseerror.description.&lt;code&gt;" and falls back to
/// the generic message for anything else, so a code without a matching text shows the user a
/// raw placeholder. Send Generic until those texts exist.</summary>
[Message(ServerMessages.PurchaseErrorMessageComposer)]
public sealed partial record PurchaseErrorMessageComposer(int ErrorCode)
{
    /// <summary>The unnumbered "purchase failed" alert.</summary>
    public const int Generic = 0;

    public static PurchaseErrorMessageComposer Failed() => new(Generic);
}
