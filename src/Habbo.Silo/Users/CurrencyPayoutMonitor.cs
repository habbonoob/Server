using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Users;

/// <summary>Pays everyone online their currencies every so often (<see cref="CurrencyOptions"/>), but not those asleep
/// in a room, who are idle; and resets a seasonal currency's balances when its season ends.</summary>
public sealed class CurrencyPayoutMonitor(
    IOptionsMonitor<CurrencyOptions> options,
    CurrencyService currencies,
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    ILogger<CurrencyPayoutMonitor> logger) : BackgroundService
{
    /// <summary>How often the clock is checked; the payout interval is counted in whole minutes.</summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private const string PayoutReason = "online payout";

    private DateTimeOffset _nextPayout = DateTimeOffset.UtcNow;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _nextPayout = DateTimeOffset.UtcNow.AddMinutes(options.CurrentValue.PayoutIntervalMinutes);
        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                await ResetEndedSeasonsAsync(now);
                if (options.CurrentValue.PayoutIntervalMinutes > 0 && now >= _nextPayout)
                {
                    _nextPayout = now.AddMinutes(options.CurrentValue.PayoutIntervalMinutes);
                    await PayOutAsync(now);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not pay out or reset currencies");
            }
        }
    }

    private async Task PayOutAsync(DateTimeOffset now)
    {
        var rules = options.CurrentValue;
        var payable = rules.Payouts.Keys.Concat(rules.ClubPayouts.Keys).Distinct().Where(type => currencies.IsPayable(type, now)).ToList();
        if (payable.Count == 0)
            return;

        var online = await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).GetOnlineAsync();
        var multiplier = currencies.PayoutMultiplier(now);
        var paid = 0;
        foreach (var playerId in online)
        {
            var player = grains.GetGrain<IPlayerGrain>(playerId);
            if (await IdleAsync(player, playerId))
                continue;

            var club = (await player.GetDataAsync()).ClubLevel >= ClubLevel.Club;
            foreach (var type in payable)
            {
                var amount = club && rules.ClubPayouts.TryGetValue(type, out var clubAmount) ? clubAmount : rules.Payouts.GetValueOrDefault(type);
                if (amount > 0)
                    await currencies.GiveAsync(playerId, type, amount * multiplier, PayoutReason);
            }

            paid++;
        }

        logger.LogInformation("Paid out currencies to {Paid} of {Online} online", paid, online.Count);
    }

    /// <summary>Asleep in the room they are in: idle for as long as that room puts people to sleep.</summary>
    private async Task<bool> IdleAsync(IPlayerGrain player, Guid playerId) =>
        await player.GetCurrentRoomAsync() is { } roomId && await grains.GetGrain<IGameRoomGrain>(roomId).IsAsleepAsync(playerId);

    private async Task ResetEndedSeasonsAsync(DateTimeOffset now)
    {
        var ended = options.CurrentValue.Seasons.Where(s => s.EndsAt <= now).ToList();
        if (ended.Count == 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        foreach (var season in ended)
        {
            if (await db.CurrencySeasonResets.AnyAsync(r => r.Type == season.Type && r.SeasonEndedAt == season.EndsAt))
                continue;

            // the marker goes in first, in the same transaction: of two silos that both saw no reset, the second fails on
            // the marker's key and resets nothing, rather than emptying balances already earned in the new season
            int reset;
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                var marker = new CurrencySeasonReset { Type = season.Type, SeasonEndedAt = season.EndsAt, ResetAt = now };
                db.CurrencySeasonResets.Add(marker);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    await transaction.RollbackAsync();
                    db.Entry(marker).State = EntityState.Detached;
                    continue;
                }

                reset = await db.UserCurrencies.Where(c => c.Type == season.Type && c.Amount != 0)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Amount, 0));
                await transaction.CommitAsync();
            }
            logger.LogInformation("Season of currency {Type} ended {EndsAt}: {Count} balance(s) reset", season.Type, season.EndsAt, reset);

            // those online see their purse emptied now
            foreach (var playerId in await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).GetOnlineAsync())
                await grains.GetGrain<IPlayerGrain>(playerId).SendActivityPointsAsync();
        }
    }
}
