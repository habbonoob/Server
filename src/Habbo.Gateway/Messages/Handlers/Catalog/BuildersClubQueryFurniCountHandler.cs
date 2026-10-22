using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Subscriptions;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>3669 BuildersClubQueryFurniCount — the Builders Club furni the player has out, for the warehouse's
/// "Borrowed items" and its limit.</summary>
internal sealed class BuildersClubQueryFurniCountHandler(ISubscriptionService subscriptions)
    : IMessageHandler<BuildersClubQueryFurniCountMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, BuildersClubQueryFurniCountMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await context.SendAsync(new BuildersClubFurniCountMessageComposer(await subscriptions.CountBuildersClubFurniAsync(player.Id, ct)), ct);
        return true;
    }
}
