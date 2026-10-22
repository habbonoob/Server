using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetTargetedOfferEvent — a targeted offer.</summary>
internal sealed class GetTargetedOfferHandler(ITargetedOfferService service) : IMessageHandler<GetTargetedOfferEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetTargetedOfferEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendAsync(player, message.OfferId);
        return true;
    }
}
