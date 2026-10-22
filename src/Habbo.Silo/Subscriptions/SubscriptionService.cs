using Habbo.Domain.Subscriptions;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Subscriptions;
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
        if (subscription is null)
            return new SubscriptionView(productName, 0, 0, 0, false, false, 0, 0, 0, 0);

        var remaining = subscription.ExpiresAt - now;
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        return new SubscriptionView(
            subscription.ProductName,
            (int)remaining.TotalDays,
            subscription.MemberPeriods,
            subscription.PeriodsSubscribedAhead,
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

    public async Task<int> UnhideBuildersClubRoomsAsync(Guid playerId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Rooms.Where(room => room.OwnerId == playerId && room.HiddenByBuildersClub)
            .ExecuteUpdateAsync(set => set.SetProperty(room => room.HiddenByBuildersClub, false), ct);
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

        var limits = subscription is null ? null : tiers.FirstOrDefault(tier => tier.ProductName == subscription.ProductName);
        return BuildersClubStatus(subscription?.ExpiresAt, limits?.FurniLimit ?? 0, limits?.Grace ?? TimeSpan.Zero,
            maxFurniLimit, DateTimeOffset.UtcNow);
    }

    /// <summary>A Builders Club membership as the client is told it. Through the grace period the tier's limit still
    /// holds (and building is the trial's); after it the limit is 0, as for one who never joined (Habbo, 2026-10-22:
    /// [0, 0, 500, 0]). Habbo's limit after the grace period is not measured.</summary>
    public static BuildersClubView BuildersClubStatus(
        DateTimeOffset? expiresAt, int furniLimit, TimeSpan grace, int maxFurniLimit, DateTimeOffset now)
    {
        if (expiresAt is not { } expires)
            return new BuildersClubView(0, 0, maxFurniLimit, 0);

        var remaining = expires - now;
        var withGrace = remaining + grace;
        return new BuildersClubView(
            (int)Math.Max(0, remaining.TotalSeconds),
            withGrace > TimeSpan.Zero ? furniLimit : 0,
            maxFurniLimit,
            (int)Math.Max(0, withGrace.TotalSeconds));
    }
}
