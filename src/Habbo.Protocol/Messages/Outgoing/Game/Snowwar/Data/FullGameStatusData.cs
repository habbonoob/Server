using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>FullGameStatusData — the whole game as it stands</summary>
[WireStruct]
public sealed partial record FullGameStatusData(int Unused, int RemainingTimeSeconds, int DurationInSeconds, IReadOnlyList<SnowWarGameObjectData> GameObjects, int Unused2, int NumberOfTeams, GameStatusData Status);
