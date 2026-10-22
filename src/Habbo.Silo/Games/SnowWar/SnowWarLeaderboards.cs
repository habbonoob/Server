using System.Globalization;
using Habbo.Domain.Games;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Game.Score;
using Habbo.Silo.Abstractions.Games;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Games.SnowWar;

/// <summary>The leaderboards, added up from the games' results: a player's points of all time or of one week (Monday to
/// Monday, UTC), and a group's as the sum of the points of those whose favourite group it is.</summary>
public sealed class SnowWarLeaderboards(IDbContextFactory<AppDbContext> dbFactory, PlayerMessenger messenger) : ISnowWarLeaderboards
{
    /// <summary>What marks a row as a group's rather than a player's.</summary>
    private const string GroupGender = "g";

    /// <summary>The client's "no favourite group".</summary>
    private const int NoGroup = -1;

    /// <summary>The start rank that asks for the stretch around the player.</summary>
    private const int AroundPlayer = -1;

    private const int MaxWindow = 100;

    public async Task SendTotalAsync(PlayerData player, LeaderboardQuery query)
    {
        var (entries, total) = await PlayersAsync(player, query, week: null, friendsOnly: false);
        await messenger.SendAsync(player.Id, new Game2TotalLeaderboardComposer(entries, total, query.GameTypeId));
    }

    public async Task SendFriendsAsync(PlayerData player, LeaderboardQuery query)
    {
        var (entries, total) = await PlayersAsync(player, query, week: null, friendsOnly: true);
        await messenger.SendAsync(player.Id, new Game2FriendsLeaderboardComposer(entries, total, query.GameTypeId));
    }

    public async Task SendGroupsAsync(PlayerData player, LeaderboardQuery query)
    {
        var (entries, total, favourite) = await GroupsAsync(player, query, week: null);
        await messenger.SendAsync(player.Id, new Game2TotalGroupLeaderboardComposer(entries, total, query.GameTypeId, favourite));
    }

    public async Task SendWeeklyAsync(PlayerData player, LeaderboardQuery query)
    {
        var week = await WeekAsync(query.WeekOffset);
        var (entries, total) = await PlayersAsync(player, query, week, friendsOnly: false);
        await messenger.SendAsync(player.Id, new Game2WeeklyLeaderboardComposer(week.Year, week.Number, week.MaxOffset, week.Offset,
            week.MinutesUntilReset, entries, total, query.GameTypeId));
    }

    public async Task SendWeeklyFriendsAsync(PlayerData player, LeaderboardQuery query)
    {
        var week = await WeekAsync(query.WeekOffset);
        var (entries, total) = await PlayersAsync(player, query, week, friendsOnly: true);
        await messenger.SendAsync(player.Id, new Game2WeeklyFriendsLeaderboardComposer(week.Year, week.Number, week.MaxOffset, week.Offset,
            week.MinutesUntilReset, entries, total, query.GameTypeId));
    }

    public async Task SendWeeklyGroupsAsync(PlayerData player, LeaderboardQuery query)
    {
        var week = await WeekAsync(query.WeekOffset);
        var (entries, total, favourite) = await GroupsAsync(player, query, week);
        await messenger.SendAsync(player.Id, new Game2WeeklyGroupLeaderboardComposer(week.Year, week.Number, week.MaxOffset, week.Offset,
            week.MinutesUntilReset, entries, total, query.GameTypeId, favourite));
    }

    private async Task<(List<LeaderboardEntryData> Entries, int Total)> PlayersAsync(
        PlayerData player, LeaderboardQuery query, LeaderboardWeek? week, bool friendsOnly)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var results = InWeek(db.SnowWarGameResults.AsNoTracking(), week);
        if (friendsOnly)
        {
            var circle = await db.Friendships.AsNoTracking().Where(f => f.UserId == player.Id).Select(f => f.FriendId).ToListAsync();
            circle.Add(player.Id);
            results = results.Where(r => circle.Contains(r.UserId));
        }

        var ranked = await results.GroupBy(r => r.UserId)
            .Select(g => new { UserId = g.Key, Score = g.Sum(r => r.Score) })
            .Join(db.Users, s => s.UserId, u => u.Id, (s, u) => new { s.UserId, s.Score, u.AccountId, u.Username, u.Figure, u.Gender })
            .OrderByDescending(s => s.Score).ThenBy(s => s.Username)
            .ToListAsync();

        var start = StartIndex(query, ranked.FindIndex(r => r.UserId == player.Id));
        var entries = ranked.Skip(start).Take(Window(query)).Select((r, i) => new LeaderboardEntryData(
            r.AccountId, r.Score, start + i + 1, r.Username, r.Figure, r.Gender.ToLetter())).ToList();
        return (entries, ranked.Count);
    }

    private async Task<(List<LeaderboardEntryData> Entries, int Total, int Favourite)> GroupsAsync(
        PlayerData player, LeaderboardQuery query, LeaderboardWeek? week)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var favourite = await db.Users.AsNoTracking().Where(u => u.Id == player.Id).Select(u => u.FavouriteGroupId).FirstOrDefaultAsync();
        var ranked = await InWeek(db.SnowWarGameResults.AsNoTracking(), week)
            .Join(db.Users.Where(u => u.FavouriteGroupId != null), r => r.UserId, u => u.Id, (r, u) => new { GroupId = u.FavouriteGroupId!.Value, r.Score })
            .GroupBy(r => r.GroupId)
            .Select(g => new { GroupId = g.Key, Score = g.Sum(r => r.Score) })
            .Join(db.Groups, s => s.GroupId, g => g.Id, (s, g) => new { s.GroupId, s.Score, g.Name, g.BadgeCode })
            .OrderByDescending(s => s.Score).ThenBy(s => s.Name)
            .ToListAsync();

        var start = StartIndex(query, favourite is { } mine ? ranked.FindIndex(r => r.GroupId == mine) : -1);
        var entries = ranked.Skip(start).Take(Window(query)).Select((r, i) => new LeaderboardEntryData(
            r.GroupId, r.Score, start + i + 1, r.Name, r.BadgeCode, GroupGender)).ToList();
        return (entries, ranked.Count, favourite ?? NoGroup);
    }

    private static int Window(LeaderboardQuery query) => Math.Clamp(query.WindowSize, 1, MaxWindow);

    /// <summary>Where the rows start: at the rank asked for, or so that the player (or their group) is in the middle.</summary>
    private static int StartIndex(LeaderboardQuery query, int ownIndex) =>
        query.StartRank != AroundPlayer ? Math.Max(0, query.StartRank - 1)
        : ownIndex < 0 ? 0
        : Math.Max(0, ownIndex - Window(query) / 2);

    private static IQueryable<SnowWarGameResult> InWeek(IQueryable<SnowWarGameResult> results, LeaderboardWeek? week) =>
        week is null ? results : results.Where(r => r.PlayedAt >= week.Start && r.PlayedAt < week.End);

    /// <summary>The week so many back from this one, and how far back there are results to look at.</summary>
    private async Task<LeaderboardWeek> WeekAsync(int offset)
    {
        var now = DateTimeOffset.UtcNow;
        var thisWeek = StartOfWeek(now);
        await using var db = await dbFactory.CreateDbContextAsync();
        var first = await db.SnowWarGameResults.AsNoTracking().OrderBy(r => r.PlayedAt).Select(r => (DateTimeOffset?)r.PlayedAt).FirstOrDefaultAsync();
        var maxOffset = first is { } earliest ? (int)((thisWeek - StartOfWeek(earliest)).TotalDays / 7) : 0;
        offset = Math.Clamp(offset, 0, maxOffset);

        var start = thisWeek.AddDays(-7 * offset);
        var nextReset = thisWeek.AddDays(7);
        return new LeaderboardWeek(start, start.AddDays(7), ISOWeek.GetYear(start.UtcDateTime), ISOWeek.GetWeekOfYear(start.UtcDateTime),
            maxOffset, offset, (int)(nextReset - now).TotalMinutes);
    }

    private static DateTimeOffset StartOfWeek(DateTimeOffset time)
    {
        var day = time.UtcDateTime.Date;
        var sinceMonday = ((int)day.DayOfWeek + 6) % 7;
        return new DateTimeOffset(day.AddDays(-sinceMonday), TimeSpan.Zero);
    }
}
