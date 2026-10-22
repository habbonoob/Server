using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>OneWayDoorStatusMessageComposer</summary>
[Message(ServerMessages.OneWayDoorStatusMessageComposer)]
public sealed partial record OneWayDoorStatusMessageComposer(int ItemId, int Status);
