using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Score;

/// <summary>Game2GetTotalGroupLeaderboardEvent — the all-time leaderboard of groups</summary>
[Incoming(ClientMessages.Game2GetTotalGroupLeaderboardEvent)]
public sealed partial record Game2GetTotalGroupLeaderboardEvent(int GameTypeId, int StartRank, int Direction, int ViewSize, int WindowSize);
