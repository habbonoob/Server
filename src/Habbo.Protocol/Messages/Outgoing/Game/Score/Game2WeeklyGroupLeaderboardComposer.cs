using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Score;

/// <summary>Game2WeeklyGroupLeaderboardComposer — a week's groups' leaderboard</summary>
[Message(ServerMessages.Game2WeeklyGroupLeaderboardComposer)]
public sealed partial record Game2WeeklyGroupLeaderboardComposer(int Year, int Week, int MaxOffset, int CurrentOffset, int MinutesUntilReset, IReadOnlyList<LeaderboardEntryData> Entries, int TotalListSize, int GameTypeId, int FavouriteGroupId);
