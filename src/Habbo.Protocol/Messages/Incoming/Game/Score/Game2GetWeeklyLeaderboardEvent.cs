using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Score;

/// <summary>Game2GetWeeklyLeaderboardEvent — a week's leaderboard, so many weeks back</summary>
[Incoming(ClientMessages.Game2GetWeeklyLeaderboardEvent)]
public sealed partial record Game2GetWeeklyLeaderboardEvent(int GameTypeId, int WeekOffset, int StartRank, int Direction, int ViewSize, int WindowSize);
