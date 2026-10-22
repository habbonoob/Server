using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Users;

/// <summary>Badge ownership across the hotel: how many own each badge, so how rare it is, and the leaderboards of the
/// players with the most badges, the most of each rarity, and the highest achievement level (the sum of their
/// achievements' levels). The boards are counted from the whole hotel and stand for a while before they are counted
/// again; a badge's owners are counted when asked.</summary>
public sealed class BadgeLeaderboards(
    IDbContextFactory<AppDbContext> dbFactory,
    IOptions<BadgeRarityOptions> rarity,
    IOptions<BadgeLeaderboardOptions> options)
{
    /// <summary>The boards, as the client numbers them.</summary>
    public const int TotalBadges = 0, BadgesByRarity = 1, AchievementLevel = 2;

    /// <summary>The rank of a player the board does not reach.</summary>
    public const int Unranked = -1;

    private readonly SemaphoreSlim _counting = new(1, 1);
    private BadgeStandings? _standings;

    /// <summary>How many players own each of the codes, counted now.</summary>
    public static async Task<Dictionary<string, int>> OwnersAsync(AppDbContext db, IReadOnlyCollection<string> codes) =>
        await db.UserBadges.AsNoTracking()
            .Where(b => codes.Contains(b.Code))
            .GroupBy(b => b.Code)
            .Select(g => new { Code = g.Key, Owners = g.Select(b => b.UserId).Distinct().Count() })
            .ToDictionaryAsync(g => g.Code, g => g.Owners);

    /// <summary>The owners as the hotel tells them, and the rarity, of a badge with this many owners.</summary>
    public (int Owners, BadgeRarity Rarity) Describe(int owners) => (BadgeRarityOptions.Shown(owners), rarity.Value.Of(owners));

    /// <summary>A page of a board, with the asker's own place; a board that does not exist is empty.</summary>
    public async Task<BadgeLeaderboardPage> PageAsync(Guid askerId, int type, int rarityId, int page, int pageSize)
    {
        var standings = await StandingsAsync();
        var board = standings.Boards.GetValueOrDefault((type, type == BadgesByRarity ? rarityId : Unranked)) ?? [];
        var size = Math.Clamp(pageSize, 1, options.Value.PageSizeMax);
        var entries = board.Skip(Math.Max(0, page) * size).Take(size).ToList();

        var own = board.FirstOrDefault(e => e.UserId == askerId);
        if (own is null && standings.Players.GetValueOrDefault(askerId) is { } me)
            own = new BadgeLeaderboardEntry(me.UserId, me.AccountId, me.Username, me.Figure, Unranked, Score(me, type, rarityId));
        own ??= await UnrankedAsync(askerId);
        return new BadgeLeaderboardPage(board.Count, entries, own);
    }

    /// <summary>The player's rank among those with the most badges, or Unranked.</summary>
    public async Task<int> TotalBadgesRankAsync(Guid userId) =>
        (await StandingsAsync()).Boards.GetValueOrDefault((TotalBadges, Unranked))?.FirstOrDefault(e => e.UserId == userId)?.Rank
        ?? Unranked;

    private async Task<BadgeLeaderboardEntry> UnrankedAsync(Guid userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        return new BadgeLeaderboardEntry(user.Id, user.AccountId, user.Username, user.Figure, Unranked, 0);
    }

    private static int Score(BadgePlayerStanding player, int type, int rarityId) => type switch
    {
        TotalBadges => player.TotalBadges,
        BadgesByRarity => rarityId >= 0 && rarityId < player.ByRarity.Length ? player.ByRarity[rarityId] : 0,
        AchievementLevel => player.AchievementLevel,
        _ => 0,
    };

    private async Task<BadgeStandings> StandingsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        if (_standings is { } standing && now - standing.CountedAt < TimeSpan.FromSeconds(options.Value.RefreshSeconds))
            return standing;

        await _counting.WaitAsync();
        try
        {
            if (_standings is { } counted && now - counted.CountedAt < TimeSpan.FromSeconds(options.Value.RefreshSeconds))
                return counted;
            return _standings = await CountAsync(now);
        }
        finally
        {
            _counting.Release();
        }
    }

    private async Task<BadgeStandings> CountAsync(DateTimeOffset now)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var owned = await db.UserBadges.AsNoTracking().Select(b => new { b.UserId, b.Code }).ToListAsync();
        var rarityOf = owned.GroupBy(b => b.Code)
            .ToDictionary(g => g.Key, g => rarity.Value.Of(g.Select(b => b.UserId).Distinct().Count()));
        var levels = await db.UserAchievements.AsNoTracking()
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, Level = g.Sum(a => a.Level) })
            .ToDictionaryAsync(g => g.UserId, g => g.Level);

        var ids = owned.Select(b => b.UserId).Concat(levels.Keys).Distinct().ToList();
        var users = await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.AccountId, u.Username, u.Figure })
            .ToListAsync();

        var badgesOf = owned.GroupBy(b => b.UserId).ToDictionary(g => g.Key, g => g.Select(b => b.Code).Distinct().ToList());
        var rarities = Enum.GetValues<BadgeRarity>().Length;
        var players = users.ToDictionary(u => u.Id, u =>
        {
            var codes = badgesOf.GetValueOrDefault(u.Id) ?? [];
            var byRarity = new int[rarities];
            foreach (var code in codes)
                byRarity[(int)rarityOf[code]]++;
            return new BadgePlayerStanding(u.Id, u.AccountId, u.Username, u.Figure, codes.Count, byRarity,
                levels.GetValueOrDefault(u.Id));
        });

        var boards = new Dictionary<(int Type, int Rarity), List<BadgeLeaderboardEntry>>
        {
            [(TotalBadges, Unranked)] = Board(players.Values, p => p.TotalBadges),
            [(AchievementLevel, Unranked)] = Board(players.Values, p => p.AchievementLevel),
        };
        for (var r = 0; r < rarities; r++)
        {
            var tier = r;
            boards[(BadgesByRarity, tier)] = Board(players.Values, p => p.ByRarity[tier]);
        }

        return new BadgeStandings(now, players, boards);
    }

    /// <summary>The players with a score, the highest first (the earlier account first between equals), each ranked
    /// by place, up to the board's size.</summary>
    private List<BadgeLeaderboardEntry> Board(IEnumerable<BadgePlayerStanding> players, Func<BadgePlayerStanding, int> score) =>
        players.Select(p => (Player: p, Score: score(p)))
            .Where(p => p.Score > 0)
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.Player.AccountId)
            .Take(options.Value.Size)
            .Select((p, i) => new BadgeLeaderboardEntry(p.Player.UserId, p.Player.AccountId, p.Player.Username, p.Player.Figure, i + 1, p.Score))
            .ToList();
}
