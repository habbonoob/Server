using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>ClubGiftInfoComposer — the club's monthly gifts: when the next is due, how many are waiting, and which may be picked</summary>
[Message(ServerMessages.ClubGiftInfoComposer)]
public sealed partial record ClubGiftInfoComposer(int DaysUntilNextGift, int GiftsAvailable, IReadOnlyList<CatalogOfferData> Offers, IReadOnlyList<ClubGiftData> Gifts);
