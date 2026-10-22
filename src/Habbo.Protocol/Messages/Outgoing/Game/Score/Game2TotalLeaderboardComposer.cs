using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Score;

/// <summary>Game2TotalLeaderboardComposer — the all-time leaderboard</summary>
[Message(ServerMessages.Game2TotalLeaderboardComposer)]
public sealed partial record Game2TotalLeaderboardComposer(IReadOnlyList<LeaderboardEntryData> Entries, int TotalListSize, int GameTypeId);
