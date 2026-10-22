using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>ShopTargetedOfferViewedEvent — a targeted offer was looked at.</summary>
internal sealed class ShopTargetedOfferViewedHandler(ITargetedOfferService service) : IMessageHandler<ShopTargetedOfferViewedEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ShopTargetedOfferViewedEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SetStateAsync(player, message.OfferId, message.TrackingState);
        return true;
    }
}
