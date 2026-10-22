using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Score;

/// <summary>LeaderboardEntryData — a row: a player (or a group: gender "g", the group's id, name and badge)</summary>
[WireStruct]
public sealed partial record LeaderboardEntryData(int UserId, int Score, int Rank, string Name, string Figure, string Gender);
