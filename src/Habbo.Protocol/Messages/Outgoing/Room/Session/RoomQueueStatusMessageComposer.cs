using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>RoomQueueStatusMessageComposer — where the player stands in the room's lines; the first set is the one they are in</summary>
[Message(ServerMessages.RoomQueueStatusMessageComposer)]
public sealed partial record RoomQueueStatusMessageComposer(int FlatId, IReadOnlyList<RoomQueueSetData> Sets);
