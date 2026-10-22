namespace Habbo.Silo.Users;

/// <summary>One player's place on a badge leaderboard: their rank (BadgeLeaderboards.Unranked below the board) and score.</summary>
public sealed record BadgeLeaderboardEntry(Guid UserId, int AccountId, string Username, string Figure, int Rank, int Score);
