using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>RoomDimmerGetPresetsMessageEvent</summary>
[Incoming(ClientMessages.RoomDimmerGetPresetsMessageEvent)]
public sealed partial record RoomDimmerGetPresetsMessageEvent(int ItemId);
