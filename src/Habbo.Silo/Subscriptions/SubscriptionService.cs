using Habbo.Domain.Subscriptions;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Subscriptions;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Subscriptions;

/// <summary>Reads a player's subscriptions and works the stored expiry into the counts the
/// client shows. A player with no row is reported as never having joined, which is a real
/// answer rather than a missing one.</summary>
public sealed class SubscriptionService(IDbContextFactory<AppDbContext> dbFactory) : ISubscriptionService
{
    public async Task<SubscriptionView> GetAsync(
        Guid playerId, string productName, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await db.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.UserId == playerId && row.ProductName == productName, ct);

        var now = DateTimeOffset.UtcNow;
        return subscription is null
            ? new SubscriptionView(productName, 0, 0, 0, false, false, 0, 0, 0, 0)
            : ClubStatus(subscription, now);
    }

    /// <summary>The length of one period as the client counts it: it shows a member
    /// periodsSubscribedAhead * 31 + daysToPeriodEnd days (com/sulake/habbo/toolbar/extensions/purse/PurseClubArea.as:92,
    /// com/sulake/habbo/catalog/viewer/widgets/ClubBuyCatalogWidget.as:96, build WIN63-202611031222-591562852).</summary>
    public const int DaysPerPeriod = 31;

    /// <summary>A membership as the client is told it. The days left are counted in whole days, a day begun
    /// counting as one, and split into the period running now (1 to 31 days) and the full periods after it.
    /// A new 31-day box is therefore 31 days and 0 periods ahead, with 44639 minutes left a moment after
    /// buying (habbo.com, October 2026). The client takes a member with 0 days to be out of the club
    /// (com/sulake/habbo/navigator/IncomingMessages.as:370, same build), so the last day still counts as one.</summary>
    public static SubscriptionView ClubStatus(Subscription subscription, DateTimeOffset now)
    {
        var remaining = subscription.ExpiresAt - now;
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        var daysLeft = (int)((remaining.Ticks + TimeSpan.TicksPerDay - 1) / TimeSpan.TicksPerDay);
        var periodsAhead = daysLeft == 0 ? 0 : (daysLeft - 1) / DaysPerPeriod;

        return new SubscriptionView(
            subscription.ProductName,
            daysLeft - periodsAhead * DaysPerPeriod,
            subscription.MemberPeriods,
            periodsAhead,
            // Having a row at all means they joined once, whether or not it still runs.
            HasEverBeenMember: true,
            IsVip: subscription.IsActive(now),
            subscription.PastClubDays,
            subscription.PastVipDays,
            (int)remaining.TotalMinutes,
            MinutesSinceLastModified: subscription.LastModifiedAt is { } modified
                ? (int)Math.Max(0, (now - modified).TotalMinutes)
                : 0);
    }

    public async Task<int> CountBuildersClubFurniAsync(Guid playerId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await Rooms.BuildersClubFurni.CountAsync(db, playerId, ct);
    }

    public async Task<IReadOnlyList<int>> GetBuildersClubLockedRoomsAsync(Guid playerId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Rooms.Where(room => room.OwnerId == playerId && room.HiddenByBuildersClub).Select(room => room.Id).ToListAsync(ct);
    }

    public async Task<BuildersClubView> GetBuildersClubAsync(Guid playerId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var subscription = await db.Subscriptions
            .AsNoTracking()
            .Where(row => row.UserId == playerId && row.ProductName.StartsWith(SubscriptionProducts.BuildersClubPrefix))
            .OrderByDescending(row => row.ExpiresAt)
            .FirstOrDefaultAsync(ct);

        // The ceiling is the best tier the hotel sells, so it is shown even to a non-member:
        // the client draws it as what upgrading would buy.
        var tiers = await db.BuildersClubLimits.AsNoTracking().ToListAsync(ct);
        var maxFurniLimit = tiers.Count == 0 ? 0 : tiers.Max(tier => tier.MaxFurniLimit);

        // the upgrades bought go on top of the tier's limit
        var limits = subscription is null ? null : tiers.FirstOrDefault(tier => tier.ProductName == subscription.ProductName);
        var bonus = await db.Users.AsNoTracking().Where(u => u.Id == playerId).Select(u => u.BuildersClubBonusFurni).FirstOrDefaultAsync(ct);
        return BuildersClubStatus(subscription?.ExpiresAt, (limits?.FurniLimit ?? 0) + bonus, limits?.Grace ?? TimeSpan.Zero,
            maxFurniLimit, DateTimeOffset.UtcNow);
    }

    /// <summary>A Builders Club membership as the client is told it. Through the grace period the tier's limit still
    /// holds (and building is the trial's). One with no membership, never joined or past the grace period, is on the
    /// free trial: they build with the trial's limit (<see cref="BuildersClubFurni.TrialFurniLimit"/>), and the room
    /// they build in is locked. Habbo's limit after a grace period is not measured; it is taken to be the trial's.
    /// </summary>
    public static BuildersClubView BuildersClubStatus(
        DateTimeOffset? expiresAt, int furniLimit, TimeSpan grace, int maxFurniLimit, DateTimeOffset now)
    {
        if (expiresAt is not { } expires)
            return new BuildersClubView(0, BuildersClubFurni.TrialFurniLimit, maxFurniLimit, 0);

        var remaining = expires - now;
        var withGrace = remaining + grace;
        return new BuildersClubView(
            (int)Math.Max(0, remaining.TotalSeconds),
            withGrace > TimeSpan.Zero ? furniLimit : BuildersClubFurni.TrialFurniLimit,
            maxFurniLimit,
            (int)Math.Max(0, withGrace.TotalSeconds));
    }
}
