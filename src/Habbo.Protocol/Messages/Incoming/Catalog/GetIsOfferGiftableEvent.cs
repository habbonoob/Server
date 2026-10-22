using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetIsOfferGiftableEvent</summary>
[Incoming(ClientMessages.GetIsOfferGiftableEvent)]
public sealed partial record GetIsOfferGiftableEvent(int OfferId);
