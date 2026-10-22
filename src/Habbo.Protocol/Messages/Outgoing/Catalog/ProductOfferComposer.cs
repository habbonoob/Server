using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>ProductOfferComposer</summary>
[Message(ServerMessages.ProductOfferComposer)]
public sealed partial record ProductOfferComposer(CatalogOfferData Offer);
