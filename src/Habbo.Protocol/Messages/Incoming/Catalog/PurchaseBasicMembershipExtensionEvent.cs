using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>PurchaseBasicMembershipExtensionEvent</summary>
[Incoming(ClientMessages.PurchaseBasicMembershipExtensionEvent)]
public sealed partial record PurchaseBasicMembershipExtensionEvent(int OfferId);
