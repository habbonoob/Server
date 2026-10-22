using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Score;

/// <summary>Game2GetFriendsLeaderboardEvent — the all-time leaderboard of the player and their friends</summary>
[Incoming(ClientMessages.Game2GetFriendsLeaderboardEvent)]
public sealed partial record Game2GetFriendsLeaderboardEvent(int GameTypeId, int StartRank, int Direction, int ViewSize, int WindowSize);
