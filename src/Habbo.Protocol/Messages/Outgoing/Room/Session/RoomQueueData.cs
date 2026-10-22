using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>RoomQueueData — one queue of a line: its type ("c" club, "d" everyone) and how many are ahead</summary>
[WireStruct]
public sealed partial record RoomQueueData(string Type, int Size);
