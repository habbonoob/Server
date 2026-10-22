using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Score;

/// <summary>Game2FriendsLeaderboardComposer — the friends' all-time leaderboard</summary>
[Message(ServerMessages.Game2FriendsLeaderboardComposer)]
public sealed partial record Game2FriendsLeaderboardComposer(IReadOnlyList<LeaderboardEntryData> Entries, int TotalListSize, int GameTypeId);
