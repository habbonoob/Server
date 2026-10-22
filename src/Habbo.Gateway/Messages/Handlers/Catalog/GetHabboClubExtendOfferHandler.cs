using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Subscriptions;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetHabboClubExtendOfferMessageEvent — the discounted VIP extension.</summary>
internal sealed class GetHabboClubExtendOfferHandler(IClubService service) : IMessageHandler<GetHabboClubExtendOfferMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetHabboClubExtendOfferMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendExtendOfferAsync(player, vip: true);
        return true;
    }
}
