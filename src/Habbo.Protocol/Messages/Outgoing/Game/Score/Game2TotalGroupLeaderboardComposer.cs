using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Score;

/// <summary>Game2TotalGroupLeaderboardComposer — the groups' all-time leaderboard</summary>
[Message(ServerMessages.Game2TotalGroupLeaderboardComposer)]
public sealed partial record Game2TotalGroupLeaderboardComposer(IReadOnlyList<LeaderboardEntryData> Entries, int TotalListSize, int GameTypeId, int FavouriteGroupId);
