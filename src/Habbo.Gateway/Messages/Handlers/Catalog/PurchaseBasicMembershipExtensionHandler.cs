using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Subscriptions;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>PurchaseBasicMembershipExtension — buy the discounted extension of a basic membership.</summary>
internal sealed class PurchaseBasicMembershipExtensionHandler(IClubService club, IGrainFactory grains)
    : IMessageHandler<PurchaseBasicMembershipExtensionEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PurchaseBasicMembershipExtensionEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await MembershipExtensionPurchase.RunAsync(context, club, grains, player, message.OfferId, vip: false, ct);
        return true;
    }
}
