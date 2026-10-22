using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>IsOfferGiftableMessageComposer</summary>
[Message(ServerMessages.IsOfferGiftableMessageComposer)]
public sealed partial record IsOfferGiftableMessageComposer(int OfferId, bool IsGiftable);
