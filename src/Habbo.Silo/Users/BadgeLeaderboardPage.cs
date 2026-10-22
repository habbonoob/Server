namespace Habbo.Silo.Users;

/// <summary>A page of a badge leaderboard: how many the board ranks, the page's entries, and where the asker stands.</summary>
public sealed record BadgeLeaderboardPage(int TotalEntries, IReadOnlyList<BadgeLeaderboardEntry> Entries, BadgeLeaderboardEntry Own);
