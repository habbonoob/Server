using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Subscriptions;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>PurchaseVipMembershipExtension — buy the discounted extension of a VIP membership.</summary>
internal sealed class PurchaseVipMembershipExtensionHandler(IClubService club, IGrainFactory grains)
    : IMessageHandler<PurchaseVipMembershipExtensionEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PurchaseVipMembershipExtensionEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await MembershipExtensionPurchase.RunAsync(context, club, grains, player, message.OfferId, vip: true, ct);
        return true;
    }
}
