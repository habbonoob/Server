using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Vault;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>What the player earns into the earnings window rather than the purse: the daily gift, and the duckets of the
/// window's categories, which a club member gets more of.</summary>
public sealed partial class PlayerGrain
{
    /// <summary>The daily gift, on the first login of a (UTC) day. One not yet claimed is not given again: it waits in
    /// the window, a single gift however many days go by.</summary>
    private async Task DailyGiftAsync()
    {
        var duckets = earningsOptions.Value.DailyGiftDuckets;
        if (duckets <= 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstAsync(u => u.Id == PlayerId);
        var now = DateTimeOffset.UtcNow;
        if (user.DailyGiftAt is { } given && given.UtcDateTime.Date >= now.UtcDateTime.Date)
            return;
        if (await db.IncomeRewards.AnyAsync(r => r.UserId == PlayerId && r.Category == IncomeRewardData.DailyGift))
            return;

        user.DailyGiftAt = now;
        await db.SaveChangesAsync();
        await EarnDucketsAsync(IncomeRewardData.DailyGift, duckets);
    }

    /// <summary>Duckets into one of the window's categories: "HC members get double duckets".</summary>
    private async Task EarnDucketsAsync(byte category, int duckets)
    {
        var club = (await GetDataAsync()).ClubLevel >= ClubLevel.Club;
        await earnings.EarnAsync(PlayerId, category, IncomeRewardData.Duckets,
            club ? duckets * earningsOptions.Value.ClubDucketMultiplier : duckets);
    }
}
