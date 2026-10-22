using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>PurchaseNotAllowedMessageComposer — the purchase is not allowed: 1 for club members only</summary>
[Message(ServerMessages.PurchaseNotAllowedMessageComposer)]
public sealed partial record PurchaseNotAllowedMessageComposer(int ErrorCode)
{
    // catalog.alert.purchasenotallowed.*: the club's, and anything else
    public const int ClubRequired = 1;
    public const int NotAllowed = 2;
}
