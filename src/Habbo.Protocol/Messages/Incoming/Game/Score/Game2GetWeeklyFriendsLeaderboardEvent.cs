using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Game.Score;

/// <summary>Game2GetWeeklyFriendsLeaderboardEvent — a week's leaderboard of the player and their friends</summary>
[Incoming(ClientMessages.Game2GetWeeklyFriendsLeaderboardEvent)]
public sealed partial record Game2GetWeeklyFriendsLeaderboardEvent(int GameTypeId, int WeekOffset, int StartRank, int Direction, int ViewSize, int WindowSize);
