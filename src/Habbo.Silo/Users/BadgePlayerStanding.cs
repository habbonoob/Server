namespace Habbo.Silo.Users;

/// <summary>What the badge leaderboards count of one player: their badges, those of each rarity (by the rarity's
/// number), and their achievement level.</summary>
internal sealed record BadgePlayerStanding(Guid UserId, int AccountId, string Username, string Figure, int TotalBadges,
    int[] ByRarity, int AchievementLevel);
