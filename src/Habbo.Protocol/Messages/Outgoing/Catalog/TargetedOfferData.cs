using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>TargetedOfferData</summary>
[WireStruct]
public sealed partial record TargetedOfferData(int TrackingState, int Id, string Identifier, string ProductCode, int PriceInCredits, int PriceInActivityPoints, int ActivityPointType, int PurchaseLimit, int ExpirationSeconds, string Title, string Description, string ImageUrl, string IconImageUrl, int Type, IReadOnlyList<string> SubProductCodes);
