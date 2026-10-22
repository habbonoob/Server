using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Score;

/// <summary>Game2WeeklyLeaderboardComposer — a week's leaderboard</summary>
[Message(ServerMessages.Game2WeeklyLeaderboardComposer)]
public sealed partial record Game2WeeklyLeaderboardComposer(int Year, int Week, int MaxOffset, int CurrentOffset, int MinutesUntilReset, IReadOnlyList<LeaderboardEntryData> Entries, int TotalListSize, int GameTypeId);
