using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>A player's place on a badge leaderboard: rank -1 for one the board does not reach.</summary>
[WireStruct]
public sealed partial record BadgeLeaderboardEntryData(int UserId, string UserName, string Figure, int Rank, int Score);
