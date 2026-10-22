using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>Game2SnowWarGameStats — the game's best players</summary>
[WireStruct]
public sealed partial record Game2SnowWarGameStats(int PlayerWithMostKills, int PlayerWithMostHits);
