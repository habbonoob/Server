using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>PurchaseVipMembershipExtensionEvent</summary>
[Incoming(ClientMessages.PurchaseVipMembershipExtensionEvent)]
public sealed partial record PurchaseVipMembershipExtensionEvent(int OfferId);
