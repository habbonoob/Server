using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>RoomEntryData — a room a group could live in</summary>
[WireStruct]
public sealed partial record RoomEntryData(int RoomId, string RoomName, bool HasControllers);
