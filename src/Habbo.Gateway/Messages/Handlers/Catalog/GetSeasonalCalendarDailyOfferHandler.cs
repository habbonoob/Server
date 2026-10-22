using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetSeasonalCalendarDailyOfferMessageEvent — today's calendar offer.</summary>
internal sealed class GetSeasonalCalendarDailyOfferHandler(ICatalogExtrasService service) : IMessageHandler<GetSeasonalCalendarDailyOfferMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetSeasonalCalendarDailyOfferMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendSeasonalCalendarOfferAsync(player);
        return true;
    }
}
