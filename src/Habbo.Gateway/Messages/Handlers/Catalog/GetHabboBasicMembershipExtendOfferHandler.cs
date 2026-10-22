using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Subscriptions;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetHabboBasicMembershipExtendOfferEvent — the discounted basic extension.</summary>
internal sealed class GetHabboBasicMembershipExtendOfferHandler(IClubService service) : IMessageHandler<GetHabboBasicMembershipExtendOfferEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetHabboBasicMembershipExtendOfferEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendExtendOfferAsync(player, vip: false);
        return true;
    }
}
