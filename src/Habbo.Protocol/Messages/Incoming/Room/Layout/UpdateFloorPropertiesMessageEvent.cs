using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Layout;

/// <summary>UpdateFloorPropertiesMessageEvent — the floor plan editor's save; an import leaves the wall height out</summary>
[Incoming(ClientMessages.UpdateFloorPropertiesMessageEvent)]
public sealed partial record UpdateFloorPropertiesMessageEvent(string Heightmap, int? DoorX = null, int? DoorY = null, int? DoorDirection = null, int? WallThickness = null, int? FloorThickness = null, int? FixedWallsHeight = null);
