using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Layout;

/// <summary>RoomOccupiedTilesMessageComposer — the tiles furniture stands on, which the floor plan editor keeps</summary>
[Message(ServerMessages.RoomOccupiedTilesMessageComposer)]
public sealed partial record RoomOccupiedTilesMessageComposer(IReadOnlyList<OccupiedTileData> Tiles);
