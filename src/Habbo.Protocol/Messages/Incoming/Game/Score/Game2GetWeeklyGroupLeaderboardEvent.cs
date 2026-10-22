using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Score;

/// <summary>Game2GetWeeklyGroupLeaderboardEvent — a week's leaderboard of groups</summary>
[Incoming(ClientMessages.Game2GetWeeklyGroupLeaderboardEvent)]
public sealed partial record Game2GetWeeklyGroupLeaderboardEvent(int GameTypeId, int WeekOffset, int StartRank, int Direction, int ViewSize, int WindowSize);
