using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>SetTargetedOfferStateEvent — how the client shows a targeted offer.</summary>
internal sealed class SetTargetedOfferStateHandler(ITargetedOfferService service) : IMessageHandler<SetTargetedOfferStateEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetTargetedOfferStateEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SetStateAsync(player, message.OfferId, message.State);
        return true;
    }
}
