using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>SeasonalCalendarDailyOfferMessageComposer — today's offer of the seasonal calendar</summary>
[Message(ServerMessages.SeasonalCalendarDailyOfferMessageComposer)]
public sealed partial record SeasonalCalendarDailyOfferMessageComposer(int PageId, CatalogOfferData Offer);
