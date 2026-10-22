using Habbo.Domain.Games;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Games;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Games.SnowWar;

/// <summary>A player's SnowStorm account: their skill (the level of the total score achievement, which the game center
/// shows as stars), the games they have played and have left today, and what each game adds to it.</summary>
public sealed class SnowWarAccounts(IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains, IOptionsMonitor<SnowWarOptions> options)
{
    public const string TotalScoreAchievement = "ACH_SnowWarTotalScore";
    public const string WinAchievement = "ACH_SnowStormWin_";
    public const string HitAchievement = "ACH_SnowStormHit_";
    public const string ThrowAchievement = "ACH_SnowStormThrow_";

    /// <summary>The client's "as many as you like".</summary>
    public const int Unlimited = -1;

    public async Task<SnowWarPlayer> PlayerAsync(PlayerData player, int team, int roomBeforeGame)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var achievement = await db.Achievements.AsNoTracking().Include(a => a.Levels).FirstOrDefaultAsync(a => a.Name == TotalScoreAchievement);
        var mine = achievement is null ? null
            : await db.UserAchievements.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == player.Id && u.AchievementId == achievement.Id);
        var level = mine?.Level ?? 0;
        var score = mine?.Progress ?? 0;
        var next = achievement?.Levels.Where(l => l.Level > level).OrderBy(l => l.Level).FirstOrDefault()?.RequiredProgress ?? score;
        return new SnowWarPlayer(player.Id, player.AccountId, player.Username, player.Figure, player.Gender.ToLetter(), team,
            level, score, next, roomBeforeGame);
    }

    /// <summary>How many games the player has played, and how many free ones they have left today.</summary>
    public async Task<(int Played, int FreeLeft)> GamesAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var played = await db.SnowWarGameResults.CountAsync(r => r.UserId == player.Id);
        var rules = options.CurrentValue;
        if (rules.FreeGamesPerDay <= 0 || (rules.ClubPlaysUnlimited && player.ClubLevel != ClubLevel.None))
            return (played, Unlimited);

        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var playedToday = await db.SnowWarGameResults.CountAsync(r => r.UserId == player.Id && r.PlayedAt >= today);
        var tokens = await db.Users.Where(u => u.Id == player.Id).Select(u => u.SnowWarTokens).FirstOrDefaultAsync();
        return (played, Math.Max(0, rules.FreeGamesPerDay - playedToday) + tokens);
    }

    /// <summary>Sells the player a token offer: the price taken, the tokens added. Null when it went through,
    /// otherwise what was short (credits or activity points).</summary>
    public async Task<bool?> BuyTokensAsync(PlayerData player, SnowWarTokenOffer offer)
    {
        // each part of the price is taken, not just looked at, before the next; points given back if the credits fall short
        var grain = grains.GetGrain<IPlayerGrain>(player.Id);
        if (offer.PriceInActivityPoints > 0
            && !await grain.SpendActivityPointsAsync(offer.ActivityPointType, offer.PriceInActivityPoints, TokenPurchaseReason))
            return false;
        if (offer.PriceInCredits > 0 && await grain.SpendCreditsAsync(offer.PriceInCredits, TokenPurchaseReason) is null)
        {
            if (offer.PriceInActivityPoints > 0)
                await grain.AddActivityPointsAsync(offer.ActivityPointType, offer.PriceInActivityPoints, TokenPurchaseReason);
            return true;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Users.Where(u => u.Id == player.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.SnowWarTokens, u => u.SnowWarTokens + offer.Tokens));
        return null;
    }

    /// <summary>The currency transaction reason tokens are bought with.</summary>
    private const string TokenPurchaseReason = "snowwar_tokens";

    /// <summary>Keeps the game's results, and moves each player on towards the game's achievements.</summary>
    public async Task RecordAsync(IReadOnlyList<SnowWarGameResult> results)
    {
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.SnowWarGameResults.AddRange(results);
            await db.SaveChangesAsync();

            // a game beyond the day's free ones takes a token
            var rules = options.CurrentValue;
            if (rules.FreeGamesPerDay > 0)
            {
                var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
                foreach (var result in results)
                {
                    var clubLevel = await db.Users.Where(u => u.Id == result.UserId).Select(u => u.ClubLevel).FirstAsync();
                    var playedToday = await db.SnowWarGameResults.CountAsync(r => r.UserId == result.UserId && r.PlayedAt >= today);
                    var unlimited = rules.ClubPlaysUnlimited && clubLevel != ClubLevel.None;
                    // taken in the database, so tokens bought meanwhile are not written over
                    if (!unlimited && playedToday > rules.FreeGamesPerDay)
                        await db.Users.Where(u => u.Id == result.UserId && u.SnowWarTokens > 0)
                            .ExecuteUpdateAsync(s => s.SetProperty(u => u.SnowWarTokens, u => u.SnowWarTokens - 1));
                }
            }
        }

        foreach (var result in results)
        {
            var player = grains.GetGrain<IPlayerGrain>(result.UserId);
            await ProgressAsync(player, TotalScoreAchievement, result.Score);
            await ProgressAsync(player, WinAchievement, result.Won ? 1 : 0);
            await ProgressAsync(player, HitAchievement, result.SnowballHits);
            await ProgressAsync(player, ThrowAchievement, result.SnowballsThrown);
        }
    }

    private static Task ProgressAsync(IPlayerGrain player, string achievement, int amount) =>
        amount > 0 ? player.ProgressAchievementAsync(achievement, amount) : Task.CompletedTask;
}
