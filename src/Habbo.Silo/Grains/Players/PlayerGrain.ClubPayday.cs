using Habbo.Domain.Subscriptions;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Protocol.Messages.Outgoing.Vault;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>The club's payday: every period from the day a player first joined, a member is paid
/// back a share of the credits they spent in it, plus a bonus for an unbroken membership, into the
/// earnings window's HC payday bonus.</summary>
public sealed partial class PlayerGrain
{
    /// <summary>The join date as the club center prints it.</summary>
    private const string JoinDateFormat = "dd-MM-yyyy";

    public async Task SendKickbackInfoAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await ProcessPaydaysAsync(db);

        var options = clubOptions.Value;
        var now = DateTimeOffset.UtcNow;
        var club = await ClubSubscriptionAsync(db);
        var streakDays = club?.StreakStartedAt is { } streak && club.IsActive(now) ? (int)(now - streak).TotalDays : 0;
        var (periodStart, nextPayday) = PaydayPeriod(club, now);
        var spent = periodStart is null ? 0 : await CreditsSpentAsync(db, periodStart.Value, now);
        var paydays = await db.ClubPaydays.AsNoTracking().Where(p => p.UserId == PlayerId).ToListAsync();

        Send(new ScrSendKickbackInfoMessageComposer(
            streakDays,
            club?.StartedAt?.UtcDateTime.ToString(JoinDateFormat, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            options.KickbackPercentage,
            paydays.Where(p => !p.WasMember).Sum(p => p.Total),
            paydays.Where(p => p.WasMember).Sum(p => p.Total),
            spent,
            StreakBonus(streakDays),
            SpendReward(spent),
            nextPayday is { } next ? (int)Math.Max(0, (next - now).TotalMinutes) : 0));
    }

    /// <summary>Settles every payday that has come round since the last one settled.</summary>
    private async Task ProcessPaydaysAsync(AppDbContext db)
    {
        var club = await ClubSubscriptionAsync(db);
        if (club?.StartedAt is not { } started)
            return;

        var interval = TimeSpan.FromDays(Math.Max(1, clubOptions.Value.PaydayIntervalDays));
        var last = await db.ClubPaydays.Where(p => p.UserId == PlayerId).MaxAsync(p => (DateTimeOffset?)p.PaidAt) ?? started;
        var now = DateTimeOffset.UtcNow;
        var paid = 0;

        for (var payday = last + interval; payday <= now; payday += interval)
        {
            var spent = await CreditsSpentAsync(db, payday - interval, payday);
            var wasMember = club.ExpiresAt > payday && club.StreakStartedAt is { } streak && streak <= payday;
            var streakDays = wasMember ? (int)(payday - club.StreakStartedAt!.Value).TotalDays : 0;
            var row = new ClubPayday
            {
                UserId = PlayerId,
                PaidAt = payday,
                CreditsSpent = spent,
                SpendReward = SpendReward(spent),
                StreakBonus = StreakBonus(streakDays),
                WasMember = wasMember,
            };
            db.ClubPaydays.Add(row);
            if (wasMember)
                paid += row.Total;
        }

        await db.SaveChangesAsync();
        if (paid > 0)
            await earnings.EarnAsync(PlayerId, IncomeRewardData.HabboClub, IncomeRewardData.Credits, paid);
    }

    private Task<Subscription?> ClubSubscriptionAsync(AppDbContext db) =>
        db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == PlayerId && s.ProductName == SubscriptionProducts.HabboClub);

    /// <summary>The period running now: from the last payday (or the join date) to the next.</summary>
    private (DateTimeOffset? Start, DateTimeOffset? Next) PaydayPeriod(Subscription? club, DateTimeOffset now)
    {
        if (club?.StartedAt is not { } started)
            return (null, null);

        var interval = TimeSpan.FromDays(Math.Max(1, clubOptions.Value.PaydayIntervalDays));
        var periods = Math.Floor((now - started) / interval);
        var start = started + interval * periods;
        return (start, start + interval);
    }

    private async Task<int> CreditsSpentAsync(AppDbContext db, DateTimeOffset from, DateTimeOffset to) =>
        -await db.CurrencyTransactions
            .Where(t => t.UserId == PlayerId && t.Currency == CurrencyTransaction.Credits && t.Amount < 0
                        && t.Reason == CurrencyTransactionReasons.CatalogPurchase
                        && t.CreatedAt > from && t.CreatedAt <= to)
            .SumAsync(t => t.Amount);

    private int SpendReward(int spent) => (int)Math.Floor(spent * clubOptions.Value.KickbackPercentage);

    private int StreakBonus(int streakDays) =>
        clubOptions.Value.StreakBonuses
            .Where(bonus => streakDays >= bonus.Days)
            .OrderByDescending(bonus => bonus.Days)
            .Select(bonus => bonus.Credits)
            .FirstOrDefault();
}
