using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>EnterOneWayDoorMessageEvent</summary>
[Incoming(ClientMessages.EnterOneWayDoorMessageEvent)]
public sealed partial record EnterOneWayDoorMessageEvent(int ItemId);
