using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>RoomQueueSetData — one of a room's lines: spectators (1) or visitors (2)</summary>
[WireStruct]
public sealed partial record RoomQueueSetData(string Name, int Target, IReadOnlyList<RoomQueueData> Queues);
