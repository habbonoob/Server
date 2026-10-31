using Habbo.Domain.Subscriptions;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>Builders Club membership bought from the catalog (the "Builders Club (Diamonds)" page) or unwrapped from a
/// gift: the time goes on top of what the player has left, and they are told their new status at once.</summary>
public sealed partial class PlayerGrain
{
    /// <summary>Adds the time to the player's membership, at least at the tier bought; whether it had time left
    /// before. A player holds one membership: an older row of the tier it becomes, left from an earlier one, goes.
    /// </summary>
    private async Task<bool> ExtendBuildersClubAsync(AppDbContext db, string tier, TimeSpan length, int periods)
    {
        var now = DateTimeOffset.UtcNow;
        var held = await db.Subscriptions
            .Where(s => s.UserId == PlayerId && s.ProductName.StartsWith(SubscriptionProducts.BuildersClubPrefix))
            .OrderByDescending(s => s.ExpiresAt)
            .ToListAsync();
        var membership = held.FirstOrDefault();
        if (membership is null)
        {
            membership = new Subscription { UserId = PlayerId, ProductName = tier, ExpiresAt = now };
            db.Subscriptions.Add(membership);
        }

        var tiers = await db.BuildersClubLimits.AsNoTracking().ToListAsync();
        var running = BuildersClubMembership.Extend(membership, tier, length, periods, tiers, now);
        db.Subscriptions.RemoveRange(held.Skip(1).Where(s => s.ProductName == membership.ProductName));
        return running;
    }

    /// <summary>Tells the player their new Builders Club status, and shows their rooms that building on the trial hid:
    /// "Welcome to Builders Club!" for one who had no time left, "Your Builders Club membership is extended!" for one
    /// who had. Habbo sells the membership on its website, so which it sends after a catalog purchase is not measured.
    /// </summary>
    private async Task AnnounceBuildersClubAsync(bool wasRunning)
    {
        var club = await subscriptions.GetBuildersClubAsync(PlayerId);
        Send(new BuildersClubSubscriptionStatusMessageComposer(
            club.SecondsLeft, club.FurniLimit, club.MaxFurniLimit, club.SecondsLeftWithGrace));
        await subscriptions.UnhideBuildersClubRoomsAsync(PlayerId);
        Send(new NotificationDialogMessageComposer(wasRunning ? BuildersClubExtended : BuildersClubMade,
            [new NotificationParameterData(FurniLimitParameter, club.FurniLimit.ToString(System.Globalization.CultureInfo.InvariantCulture))]));
    }

    /// <summary>An upgrade is for a member: the client offers it only with membership time left.</summary>
    private async Task<bool> BuildersClubUpgradeAcceptedAsync(ShopOffer offer) =>
        !offer.Products.Any(p => p.Type == ProductTypes.BuildersClub && p.ExtraParam == SubscriptionProducts.BuildersClubAddon)
        || (await subscriptions.GetBuildersClubAsync(PlayerId)).SecondsLeft > 0;

    /// <summary>Tells the player their Builders Club status as it now is: an upgrade raised the limit.</summary>
    private async Task SendBuildersClubStatusAsync()
    {
        var club = await subscriptions.GetBuildersClubAsync(PlayerId);
        Send(new BuildersClubSubscriptionStatusMessageComposer(
            club.SecondsLeft, club.FurniLimit, club.MaxFurniLimit, club.SecondsLeftWithGrace));
    }

    private const string BuildersClubMade = "builders_club.membership_made";
    private const string BuildersClubExtended = "builders_club.membership_extended";
    private const string FurniLimitParameter = "FURNI_LIMIT";
}
