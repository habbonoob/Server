using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>ClubOfferData — one membership the club sells, and when it would then run to</summary>
[WireStruct]
public sealed partial record ClubOfferData(int OfferId, string ProductCode, bool Unused, int PriceCredits, int PriceActivityPoints, int ActivityPointType, bool Vip, int Months, int ExtraDays, bool Giftable, int DaysLeftAfterPurchase, int Year, int Month, int Day);
