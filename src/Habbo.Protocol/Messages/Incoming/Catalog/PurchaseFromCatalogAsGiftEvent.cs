using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>PurchaseFromCatalogAsGiftEvent — buy an offer wrapped as a gift for someone else</summary>
[Incoming(ClientMessages.PurchaseFromCatalogAsGiftEvent)]
public sealed partial record PurchaseFromCatalogAsGiftEvent(int PageId, int OfferId, string ExtraData, string ReceiverName, string Message, int SpriteId, int BoxId, int RibbonId, bool ShowPurchaserName);
