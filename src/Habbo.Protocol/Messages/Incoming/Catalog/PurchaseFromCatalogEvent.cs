using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>1962 PurchaseFromCatalog — buy an offer. ExtraData carries whatever the offer's
/// own UI collected (a badge code, a written message), and is empty for plain furniture.
/// Amount is the client's quantity spinner, which we clamp rather than trust.</summary>
[Incoming(ClientMessages.PurchaseFromCatalogEvent)]
public sealed partial record PurchaseFromCatalogEvent(
    int PageId, int OfferId, string ExtraData, int Amount);
