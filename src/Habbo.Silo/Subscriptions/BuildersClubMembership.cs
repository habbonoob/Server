using Habbo.Domain.Subscriptions;

namespace Habbo.Silo.Subscriptions;

/// <summary>Builders Club membership bought: "Still have some days left on your membership? The new days will just be
/// added on top - you'll never pay double." It runs on from where it ends, or from now when it has no time left (in
/// the grace period too), and its tier is the better of the one held and the one bought, so buying never lowers the
/// limit.</summary>
public static class BuildersClubMembership
{
    /// <summary>Adds the time to the membership, made the tier bought when that one allows more; whether it had time
    /// left before.</summary>
    public static bool Extend(
        Subscription membership, string tier, TimeSpan length, int periods,
        IReadOnlyCollection<BuildersClubLimits> tiers, DateTimeOffset now)
    {
        var running = membership.IsActive(now);
        if (!running)
            membership.ExpiresAt = now;

        if (LimitOf(tier, tiers) > LimitOf(membership.ProductName, tiers))
            membership.ProductName = tier;

        membership.ExpiresAt += length;
        membership.MemberPeriods += periods;
        membership.StartedAt ??= now;
        membership.LastModifiedAt = now;
        return running;
    }

    /// <summary>The furni limit a tier allows; nothing for one the hotel has no limits for.</summary>
    private static int LimitOf(string tier, IReadOnlyCollection<BuildersClubLimits> tiers) =>
        tiers.FirstOrDefault(limits => limits.ProductName == tier)?.FurniLimit ?? 0;
}
