using Habbo.Domain.Achievements;
using Habbo.Domain.Subscriptions;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Inventory.Achievements;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.Vault;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>Working towards achievements: progress adds up, and each level reached pays its badge (in
/// place of the level before's), its points in the achievement's currency (duckets into the earnings
/// window) and towards the player's score, and tells the player.</summary>
public sealed partial class PlayerGrain
{
    private const string AchievementReason = "achievement";

    /// <summary>A level-up the client opens its dialog for.</summary>
    private const bool ShowLevelUpDialog = true;

    private const int NoBonusPoints = 0;

    /// <summary>What a login moves on: days in a row, days since registering, the name chosen at registration, and
    /// club membership.</summary>
    private async Task LoginAchievementsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == PlayerId);
        await SetAchievementProgressAsync(Achievements.AchievementNames.Login, user.LoginStreakDays);
        await ReachAchievementAsync(Achievements.AchievementNames.RegistrationDuration, (int)(DateTimeOffset.UtcNow - user.CreatedAt).TotalDays);
        await ReachAchievementAsync(Achievements.AchievementNames.Name, 1);
        if (InHappyHour(DateTimeOffset.UtcNow))
            await ReachAchievementAsync(Achievements.AchievementNames.HappyHour, 1);
        await ClubAchievementsAsync(db);
        await CountChatStylesAsync(db);
    }

    /// <summary>Happy hour: 19:00 to 20:00 UTC on weekdays, 17:00 to 18:00 UTC at weekends.</summary>
    private static bool InHappyHour(DateTimeOffset now) =>
        now.UtcDateTime.Hour == (now.UtcDateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 17 : 19);

    private async Task CountChatStylesAsync(AppDbContext db) =>
        await ReachAchievementAsync(Achievements.AchievementNames.ChatStyleOwner, await db.UserChatStyles.CountAsync(s => s.UserId == PlayerId));

    /// <summary>Habbo Club days added to the account (those of past runs, and all of the run going now, to its end) and the
    /// periods paid for.</summary>
    private async Task ClubAchievementsAsync(AppDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        if (await db.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == PlayerId && s.ProductName == SubscriptionProducts.HabboClub)
            is not { } club)
            return;

        var running = club.IsActive(now) && club.StreakStartedAt is { } started ? (int)(club.ExpiresAt - started).TotalDays : 0;
        await ReachAchievementAsync(Achievements.AchievementNames.HabboClub, club.PastClubDays + club.PastVipDays + running);
        await ReachAchievementAsync(Achievements.AchievementNames.BasicClub, club.MemberPeriods);
    }

    /// <summary>Every heartbeat online: its minutes in the hotel.</summary>
    private Task OnlineAchievementsAsync() =>
        ProgressAchievementAsync(Achievements.AchievementNames.AllTimeHotelPresence, (int)OnlineHeartbeat.TotalMinutes);

    public async Task CountPetsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var plants = await db.Pets.AsNoTracking().Include(p => p.Type).ThenInclude(t => t!.Layers)
            .Where(p => p.OwnerId == PlayerId && p.TypeId == Achievements.PetAchievements.MonsterPlant).ToListAsync();
        await ReachAchievementAsync(Achievements.AchievementNames.MonsterPlantColorCollector, plants.Select(p => p.PaletteId).Distinct().Count());
        await ReachAchievementAsync(Achievements.AchievementNames.MonsterPlantShapeCollector,
            plants.Select(p => Pets.PlantLooks.PartOn(p.Type!, p.CustomParts, Pets.PlantLooks.BodyTag)).Distinct().Count());
    }

    /// <summary>For an achievement whose progress may start over (a streak broken): its progress is the value; the levels
    /// reached stay.</summary>
    private Task SetAchievementProgressAsync(string achievementName, int value) =>
        ProgressAchievementAsync(achievementName, _ => Math.Max(0, value));

    public Task ProgressAchievementAsync(string achievementName, int amount) =>
        amount <= 0 ? Task.CompletedTask : ProgressAchievementAsync(achievementName, progress => progress + amount);

    /// <summary>For an achievement that counts something the player has (friends, days, furni in a room): its progress
    /// rises to the total when the total is more; it never falls.</summary>
    public Task ReachAchievementAsync(string achievementName, int total) =>
        total <= 0 ? Task.CompletedTask : ProgressAchievementAsync(achievementName, progress => Math.Max(progress, total));

    private async Task ProgressAchievementAsync(string achievementName, Func<int, int> progressed)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var achievement = await db.Achievements.AsNoTracking().Include(a => a.Levels).FirstOrDefaultAsync(a => a.Name == achievementName);
        if (achievement is null || achievement.Levels.Count == 0)
            return;

        var levelCount = achievement.TotalLevels;
        var row = await db.UserAchievements.FirstOrDefaultAsync(r => r.UserId == PlayerId && r.AchievementId == achievement.Id);
        if (row is null)
            db.UserAchievements.Add(row = new UserAchievement { UserId = PlayerId, AchievementId = achievement.Id });
        if (row.Level >= levelCount)
            return;

        var progressBefore = row.Progress;
        row.Progress = progressed(row.Progress);
        if (row.Progress == progressBefore)
            return;
        var reached = new List<AchievementLevel>();
        while (achievement.Levels.FirstOrDefault(l => l.Level == row.Level + 1) is { } next && row.Progress >= next.RequiredProgress)
        {
            row.Level = next.Level;
            reached.Add(next);
        }

        var user = await db.Users.FirstAsync(u => u.Id == PlayerId);
        user.AchievementScore += reached.Sum(l => l.RewardPoints);
        await db.SaveChangesAsync();
        Data = Data is null ? null : Data with { AchievementScore = user.AchievementScore };

        foreach (var level in reached)
        {
            var badge = achievement.BadgeOf(level);
            var previous = achievement.Levels.FirstOrDefault(l => l.Level == level.Level - 1) is { } before ? achievement.BadgeOf(before) : string.Empty;
            if (previous.Length > 0)
                await RemoveBadgeAsync(previous);
            await GiveBadgeAsync(badge);
            // duckets wait in the earnings window; the level-up still tells the level's own amount
            if (level.RewardPoints > 0 && achievement.RewardPointType == IncomeRewardData.Duckets)
                await EarnDucketsAsync(IncomeRewardData.Achievements, level.RewardPoints);
            else if (level.RewardPoints > 0)
                await AddActivityPointsAsync(achievement.RewardPointType, level.RewardPoints, AchievementReason);

            var badgeId = await db.UserBadges.Where(b => b.UserId == PlayerId && b.Code == badge).Select(b => b.Id).FirstOrDefaultAsync();
            var (owners, rarity) = await DescribeBadgeAsync(db, badge);
            Send(new HabboAchievementNotificationMessageComposer(new AchievementLevelUpData(
                achievement.Id, level.Level, badgeId, badge, level.RewardPoints, level.RewardPoints, achievement.RewardPointType, NoBonusPoints,
                achievement.Id, previous, achievement.Category, ShowLevelUpDialog, owners, rarity)));
            await friendActivity.AchievementReachedAsync(PlayerId, badge);
        }

        if (reached.Count > 0)
            await MeetResolutionsAsync(achievement.Id, reached[^1].Level, user.Username);

        if (await achievements.GetProgressAsync(PlayerId, achievement.Id) is { } view)
            Send(new AchievementComposer(AchievementData.From(view)));
        if (reached.Count > 0)
            Send(new AchievementsScoreComposer(user.AchievementScore));
    }
}
