using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>An offer of SnowStorm game tokens: the client knows it by its localization id (GET_SNOWWAR_TOKENS, 2 or 3).</summary>
[WireStruct]
public sealed partial record SnowWarGameTokenOfferData(int OfferId, string LocalizationId, int PriceInCredits, int PriceInActivityPoints, int ActivityPointType);
