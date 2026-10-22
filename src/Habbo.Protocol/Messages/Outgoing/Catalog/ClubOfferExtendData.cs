using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>ClubOfferExtendData — a membership offered at a discount to one about to run out</summary>
[WireStruct]
public sealed partial record ClubOfferExtendData(ClubOfferData Offer, int OriginalPriceCredits, int OriginalPriceActivityPoints, int OriginalActivityPointType, int SubscriptionDaysLeft);
