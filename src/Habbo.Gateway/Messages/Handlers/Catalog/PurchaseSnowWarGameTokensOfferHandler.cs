using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Games;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>551 PurchaseSnowWarGameTokensOffer — SnowStorm game tokens.</summary>
internal sealed class PurchaseSnowWarGameTokensOfferHandler(IGrainFactory grains) : IMessageHandler<PurchaseSnowWarGameTokensOfferEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PurchaseSnowWarGameTokensOfferEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await grains.GetGrain<ISnowWarDirectoryGrain>(ISnowWarDirectoryGrain.Key).BuyTokensAsync(player, message.OfferId);
        return true;
    }
}
