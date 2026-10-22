using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>RoomDimmerChangeStateMessageEvent — switch the dimmer on or off</summary>
[Incoming(ClientMessages.RoomDimmerChangeStateMessageEvent)]
public sealed partial record RoomDimmerChangeStateMessageEvent(int ItemId);
