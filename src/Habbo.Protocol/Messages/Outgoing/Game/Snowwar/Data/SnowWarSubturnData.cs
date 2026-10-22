using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>SnowWarSubturnData — what happens in one subturn</summary>
[WireStruct]
public sealed partial record SnowWarSubturnData(IReadOnlyList<SnowWarGameEventData> Events);
