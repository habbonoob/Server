using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Layout;

/// <summary>OccupiedTileData</summary>
[WireStruct]
public sealed partial record OccupiedTileData(int X, int Y);
