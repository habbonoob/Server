using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>RoomForwardMessageComposer — send the client into a room</summary>
[Message(ServerMessages.RoomForwardMessageComposer)]
public sealed partial record RoomForwardMessageComposer(int RoomId);
