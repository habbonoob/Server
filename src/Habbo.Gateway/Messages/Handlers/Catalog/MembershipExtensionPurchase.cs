using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Subscriptions;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>Buying the club's discounted extension, for either kind of membership: the refusal the
/// client alerts with, or the new balance and the player as the membership left them.</summary>
internal static class MembershipExtensionPurchase
{
    public static async Task RunAsync(GatewaySessionContext context, IClubService club, IGrainFactory grains,
        PlayerData player, int offerId, bool vip, CancellationToken ct)
    {
        var result = await club.PurchaseExtensionAsync(player, offerId, vip);
        if (!result.Success)
        {
            await context.SendAsync(CatalogRefusals.For(result), ct);
            return;
        }

        context.Session.RefreshPlayer(await grains.GetGrain<IPlayerGrain>(player.Id).GetDataAsync());
        await context.SendAsync(new CreditBalanceComposer(result.RemainingCredits), ct);
    }
}
