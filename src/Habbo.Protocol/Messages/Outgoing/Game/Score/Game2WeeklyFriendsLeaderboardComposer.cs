using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Score;

/// <summary>Game2WeeklyFriendsLeaderboardComposer — a week's friends' leaderboard</summary>
[Message(ServerMessages.Game2WeeklyFriendsLeaderboardComposer)]
public sealed partial record Game2WeeklyFriendsLeaderboardComposer(int Year, int Week, int MaxOffset, int CurrentOffset, int MinutesUntilReset, IReadOnlyList<LeaderboardEntryData> Entries, int TotalListSize, int GameTypeId);
