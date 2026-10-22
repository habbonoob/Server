using Habbo.Protocol.Messages.Incoming.PeerService;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.PeerService;

/// <summary>ClaimProductMessageEvent — claim a free product.</summary>
internal sealed class ClaimProductHandler(IProductClaimService service) : IMessageHandler<ClaimProductMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ClaimProductMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ClaimAsync(player, message.ClaimId);
        return true;
    }
}
