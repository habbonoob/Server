using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>116 PurchaseOK — the purchase went through. The client closes its spinner on this;
/// the new item arrives separately on the inventory channel. Habbo ends it with the ids of the furniture handed over,
/// which the client reads no further than the offer; nothing handed to the buyer leaves the list empty.</summary>
[Message(ServerMessages.PurchaseOKMessageComposer)]
public sealed partial record PurchaseOKMessageComposer(PurchaseOfferData Offer, IReadOnlyList<int> ItemIds);
