using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Score;

/// <summary>Game2GetTotalLeaderboardEvent — the all-time leaderboard, from a rank (-1: around the player)</summary>
[Incoming(ClientMessages.Game2GetTotalLeaderboardEvent)]
public sealed partial record Game2GetTotalLeaderboardEvent(int GameTypeId, int StartRank, int Direction, int ViewSize, int WindowSize);
