namespace Habbo.Silo.Games.SnowWar;

/// <summary>A leaderboard week: from Monday to Monday (UTC), its ISO year and number, how many weeks back it is and can
/// go, and how long until this week's board starts over.</summary>
public sealed record LeaderboardWeek(
    DateTimeOffset Start, DateTimeOffset End, int Year, int Number, int MaxOffset, int Offset, int MinutesUntilReset);
