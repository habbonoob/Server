using Habbo.Domain.Achievements;
using Habbo.Domain.Furniture.Data;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Game.Lobby;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Achievements;

/// <summary>Resolution statues' challenges: what a statue offers its owner, taking one on, how far it has got, and
/// which are done when a level is reached. The statues themselves are changed by whoever holds them.</summary>
public sealed class AchievementResolutions(IDbContextFactory<AppDbContext> dbFactory, IOptions<AchievementResolutionOptions> options)
{
    private AchievementResolutionOptions Options => options.Value;

    /// <summary>A challenge that was met: the statue and the badge to engrave on it.</summary>
    public sealed record Met(int ItemId, string Badge);

    /// <summary>The achievements a statue offers the player, each with the level they are on, the level the challenge
    /// asks for and its badge, and whether it can be taken on.</summary>
    public async Task<AchievementResolutionsMessageComposer> OffersAsync(Guid playerId, int itemId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var achievements = await OfferedAsync(db);
        var levels = await db.UserAchievements.AsNoTracking().Where(r => r.UserId == playerId).ToDictionaryAsync(r => r.AchievementId, r => r.Level);
        var challenged = await db.FurniAchievementResolutions.AsNoTracking().Where(r => r.UserId == playerId)
            .Select(r => r.AchievementId).ToListAsync();

        var offers = new List<AchievementResolutionData>();
        foreach (var achievement in achievements)
        {
            var level = levels.GetValueOrDefault(achievement.Id);
            var target = Target(achievement, level);
            var availability = level >= achievement.TotalLevels ? AchievementResolutionAvailability.AllLevelsDone
                : challenged.Contains(achievement.Id) ? AchievementResolutionAvailability.AlreadyChallenged
                : AchievementResolutionAvailability.Available;
            offers.Add(new AchievementResolutionData(achievement.Id, level, achievement.BadgeOf(target), target.Level, (int)availability));
        }

        return new AchievementResolutionsMessageComposer(itemId, offers, (int)Options.ChallengeDuration.TotalSeconds);
    }

    /// <summary>Takes the challenge on for the statue, when the achievement is one it offers and the player can take
    /// it on; null otherwise.</summary>
    public async Task<FurniAchievementResolution?> StartAsync(Guid playerId, int itemId, int achievementId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if ((await OfferedAsync(db)).FirstOrDefault(a => a.Id == achievementId) is not { } achievement
            || await db.FurniAchievementResolutions.AnyAsync(r => r.ItemId == itemId || (r.UserId == playerId && r.AchievementId == achievementId)))
            return null;

        var level = await db.UserAchievements.Where(r => r.UserId == playerId && r.AchievementId == achievementId).Select(r => r.Level).FirstOrDefaultAsync();
        if (level >= achievement.TotalLevels)
            return null;

        var now = DateTimeOffset.UtcNow;
        var resolution = new FurniAchievementResolution
        {
            ItemId = itemId, UserId = playerId, AchievementId = achievementId, TargetLevel = Target(achievement, level).Level,
            StartedAt = now, EndsAt = now + Options.ChallengeDuration,
        };
        db.FurniAchievementResolutions.Add(resolution);
        await db.SaveChangesAsync();
        return resolution;
    }

    public async Task<FurniAchievementResolution?> FindAsync(int itemId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.FurniAchievementResolutions.AsNoTracking().FirstOrDefaultAsync(r => r.ItemId == itemId);
    }

    /// <summary>The statues' challenges that are under way, of those given.</summary>
    public async Task<IReadOnlyList<FurniAchievementResolution>> UnderWayAsync(IReadOnlyCollection<int> itemIds)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.FurniAchievementResolutions.AsNoTracking().Where(r => itemIds.Contains(r.ItemId)).ToListAsync();
    }

    /// <summary>How far the challenge has got: the player's progress against what the target level needs.</summary>
    public async Task<AchievementResolutionProgressMessageComposer?> ProgressAsync(FurniAchievementResolution resolution)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var achievement = await db.Achievements.AsNoTracking().Include(a => a.Levels).FirstOrDefaultAsync(a => a.Id == resolution.AchievementId);
        if (achievement?.Levels.FirstOrDefault(l => l.Level == resolution.TargetLevel) is not { } target)
            return null;

        var progress = await db.UserAchievements.Where(r => r.UserId == resolution.UserId && r.AchievementId == resolution.AchievementId)
            .Select(r => r.Progress).FirstOrDefaultAsync();
        var left = Math.Max(0, (int)(resolution.EndsAt - DateTimeOffset.UtcNow).TotalSeconds);
        return new AchievementResolutionProgressMessageComposer(
            resolution.ItemId, resolution.AchievementId, achievement.BadgeOf(target), Math.Min(progress, target.RequiredProgress), target.RequiredProgress, left);
    }

    /// <summary>Ends a statue's challenge, whatever became of it.</summary>
    public async Task EndAsync(int itemId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.FurniAchievementResolutions.Where(r => r.ItemId == itemId).ExecuteDeleteAsync();
    }

    /// <summary>The player reached a level: the challenges on the achievement that asked for it or less, and are still
    /// in time, are met and ended. One whose time is up is left for its statue to fail.</summary>
    public async Task<IReadOnlyList<Met>> LevelReachedAsync(Guid playerId, int achievementId, int level)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var met = await db.FurniAchievementResolutions
            .Where(r => r.UserId == playerId && r.AchievementId == achievementId && r.TargetLevel <= level && r.EndsAt > now)
            .ToListAsync();
        if (met.Count == 0)
            return [];

        var achievement = await db.Achievements.AsNoTracking().Include(a => a.Levels).FirstAsync(a => a.Id == achievementId);
        db.FurniAchievementResolutions.RemoveRange(met);
        await db.SaveChangesAsync();
        return met.Select(r => new Met(r.ItemId, achievement.Levels.FirstOrDefault(l => l.Level == r.TargetLevel) is { } target
            ? achievement.BadgeOf(target)
            : achievement.Name + r.TargetLevel)).ToList();
    }

    /// <summary>The live achievements with levels on record, of the categories the hotel names.</summary>
    private async Task<List<Achievement>> OfferedAsync(AppDbContext db)
    {
        var categories = Options.Categories;
        return await db.Achievements.AsNoTracking().Include(a => a.Levels)
            .Where(a => a.State == AchievementState.Active && a.Category != string.Empty && a.Levels.Count > 0)
            .Where(a => categories.Count == 0 || categories.Contains(a.Category))
            .OrderBy(a => a.Id)
            .ToListAsync();
    }

    /// <summary>The level the challenge asks for: so many beyond the one the player is on, as far as the levels on
    /// record go.</summary>
    private AchievementLevel Target(Achievement achievement, int level)
    {
        var levels = achievement.Levels.OrderBy(l => l.Level).ToList();
        return levels.LastOrDefault(l => l.Level <= level + Math.Max(1, Options.LevelsToGain) && l.Level > level) ?? levels[^1];
    }
}
