using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Marketplace;

/// <summary>MarketplaceDayStatData</summary>
[WireStruct]
public sealed partial record MarketplaceDayStatData(int DayOffset, int AveragePrice, int SoldAmount);
