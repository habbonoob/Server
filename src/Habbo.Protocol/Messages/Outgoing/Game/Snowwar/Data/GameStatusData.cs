using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>GameStatusData — a turn's checksum and the next turn's events, subturn by subturn</summary>
[WireStruct]
public sealed partial record GameStatusData(int Turn, int Checksum, IReadOnlyList<SnowWarSubturnData> Subturns);
