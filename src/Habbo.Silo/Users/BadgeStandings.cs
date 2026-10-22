namespace Habbo.Silo.Users;

/// <summary>The badge leaderboards as counted at one moment: every player's standing, and each board (by its type and,
/// for the rarity boards, the rarity) ranked.</summary>
internal sealed record BadgeStandings(DateTimeOffset CountedAt, Dictionary<Guid, BadgePlayerStanding> Players,
    Dictionary<(int Type, int Rarity), List<BadgeLeaderboardEntry>> Boards);
