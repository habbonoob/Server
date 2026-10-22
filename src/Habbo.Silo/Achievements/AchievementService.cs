using Habbo.Domain.Achievements;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Achievements;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Achievements;

/// <summary>Resolves a player's achievement progress against the level table. A player only
/// appears against an achievement once they have made a start on it, so what comes back is what
/// the client should be told about and nothing else.</summary>
public sealed class AchievementService(IDbContextFactory<AppDbContext> dbFactory) : IAchievementService
{
    public async Task<IReadOnlyList<AchievementProgressView>> GetProgressAsync(
        Guid playerId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var progress = await db.UserAchievements
            .AsNoTracking()
            .Where(row => row.UserId == playerId)
            .Include(row => row.Achievement!)
            .ThenInclude(achievement => achievement.Levels)
            .ToListAsync(ct);

        return progress
            .Select(ToView)
            .OfType<AchievementProgressView>()
            .OrderBy(view => view.AchievementId)
            .ToList();
    }

    public async Task<IReadOnlyList<AchievementProgressView>> GetAllAsync(
        Guid playerId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var achievements = await db.Achievements
            .AsNoTracking()
            .Include(achievement => achievement.Levels)
            .OrderBy(achievement => achievement.Id)
            .ToListAsync(ct);

        var progress = await db.UserAchievements
            .AsNoTracking()
            .Where(row => row.UserId == playerId)
            .ToDictionaryAsync(row => row.AchievementId, ct);

        return achievements
            .Select(achievement => ToView(
                progress.GetValueOrDefault(achievement.Id) ?? Unstarted(achievement, playerId), achievement))
            .OfType<AchievementProgressView>()
            .ToList();
    }

    public async Task<AchievementProgressView?> GetProgressAsync(Guid playerId, int achievementId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var achievement = await db.Achievements.AsNoTracking().Include(a => a.Levels).FirstOrDefaultAsync(a => a.Id == achievementId, ct);
        if (achievement is null)
            return null;

        var progress = await db.UserAchievements.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == playerId && r.AchievementId == achievementId, ct);
        return ToView(progress ?? Unstarted(achievement, playerId), achievement);
    }

    /// <summary>What an achievement nobody has started looks like: on its first level, nothing
    /// done. The client needs the entry all the same or the achievement is missing from the
    /// dialog entirely.</summary>
    private static UserAchievement Unstarted(Achievement achievement, Guid playerId) => new()
    {
        UserId = playerId,
        AchievementId = achievement.Id,
        Level = 0,
        Progress = 0,
    };

    public async Task<int> GetScoreAsync(Guid playerId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Every level up to and including the one reached has been finished and paid out.
        return await db.UserAchievements
            .AsNoTracking()
            .Where(row => row.UserId == playerId)
            .SelectMany(row => db.AchievementLevels
                .Where(level => level.AchievementId == row.AchievementId && level.Level <= row.Level)
                .Select(level => level.RewardPoints))
            .SumAsync(ct);
    }

    private static AchievementProgressView? ToView(UserAchievement progress) =>
        progress.Achievement is { } achievement ? ToView(progress, achievement) : null;

    /// <summary>Null when the achievement has no levels at all, which is a broken definition
    /// rather than something to report.</summary>
    private static AchievementProgressView? ToView(UserAchievement progress, Achievement achievement)
    {
        if (achievement.Levels.Count == 0)
            return null;

        var levels = achievement.Levels.OrderBy(level => level.Level).ToList();

        // What the client draws the "x of y" against. An achievement whose later levels are not
        // on record still says how many it has, so the count is the achievement's own.
        var levelCount = Math.Max(achievement.LevelCount, levels.Max(level => level.Level));
        var level = AchievementLevelProgress.Resolve(levels, progress.Level, levelCount);

        return new AchievementProgressView(
            achievement.Id,
            level.Level,
            // A level whose own badge is not on record falls back to the usual shape.
            level.BadgeCode.Length > 0 ? level.BadgeCode : achievement.Name + level.Level,
            level.StartOfLevel,
            level.ScoreLimit,
            level.RewardPoints,
            achievement.RewardPointType,
            progress.Progress,
            level.IsFinal,
            achievement.Category,
            achievement.SubCategory,
            levelCount,
            achievement.DisplayMethod,
            achievement.State);
    }
}
