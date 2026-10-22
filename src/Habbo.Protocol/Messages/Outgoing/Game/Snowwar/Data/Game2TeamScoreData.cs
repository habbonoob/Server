using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>Game2TeamScoreData — a team in the results</summary>
[WireStruct]
public sealed partial record Game2TeamScoreData(int TeamReference, int Score, IReadOnlyList<Game2TeamPlayerData> Players);
