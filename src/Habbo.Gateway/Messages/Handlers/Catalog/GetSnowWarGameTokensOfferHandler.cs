using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Games;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>3344 GetSnowWarGameTokensOffer — SnowStorm game tokens.</summary>
internal sealed class GetSnowWarGameTokensOfferHandler(IGrainFactory grains) : IMessageHandler<GetSnowWarGameTokensOfferEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetSnowWarGameTokensOfferEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await grains.GetGrain<ISnowWarDirectoryGrain>(ISnowWarDirectoryGrain.Key).SendTokenOffersAsync(player);
        return true;
    }
}
