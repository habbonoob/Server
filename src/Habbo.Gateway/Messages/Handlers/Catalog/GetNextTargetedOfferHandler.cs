using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetNextTargetedOfferEvent — the next targeted offer.</summary>
internal sealed class GetNextTargetedOfferHandler(ITargetedOfferService service) : IMessageHandler<GetNextTargetedOfferEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetNextTargetedOfferEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendNextAsync(player);
        return true;
    }
}
