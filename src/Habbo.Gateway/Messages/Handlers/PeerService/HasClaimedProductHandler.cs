using Habbo.Protocol.Messages.Incoming.PeerService;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.PeerService;

/// <summary>HasClaimedProductMessageEvent — whether a free product was claimed.</summary>
internal sealed class HasClaimedProductHandler(IProductClaimService service) : IMessageHandler<HasClaimedProductMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, HasClaimedProductMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendHasClaimedAsync(player, message.ClaimId);
        return true;
    }
}
