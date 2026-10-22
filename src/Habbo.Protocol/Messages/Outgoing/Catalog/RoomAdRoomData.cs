using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>RoomAdRoomData</summary>
[WireStruct]
public sealed partial record RoomAdRoomData(int FlatId, string RoomName, bool HasControllers);
