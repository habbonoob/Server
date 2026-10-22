using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceConfigurationComposer</summary>
[Message(ServerMessages.MarketplaceConfigurationComposer)]
public sealed partial record MarketplaceConfigurationComposer(bool IsEnabled, int Commission, int TokenBatchPrice, int TokenBatchSize, int OfferMinPrice, int OfferMaxPrice, int ExpirationHours, int AveragePricePeriod, int SellingFeePercentage, int RevenueLimit, int HalfTaxLimit);
