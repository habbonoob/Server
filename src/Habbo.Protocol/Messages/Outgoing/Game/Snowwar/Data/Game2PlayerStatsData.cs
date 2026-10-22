using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>Game2PlayerStatsData — one player's game</summary>
[WireStruct]
public sealed partial record Game2PlayerStatsData(int Score, int Kills, int Deaths, int SnowballHits, int SnowballHitsTaken, int SnowballsThrown, int SnowballsCreated, int SnowballsFromMachine, int FriendlyHits, int FriendlyKills);
