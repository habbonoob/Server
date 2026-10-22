using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>Game2TeamPlayerData — a player in the results</summary>
[WireStruct]
public sealed partial record Game2TeamPlayerData(string UserName, int UserId, string Figure, string Gender, int Score, Game2PlayerStatsData Stats);
