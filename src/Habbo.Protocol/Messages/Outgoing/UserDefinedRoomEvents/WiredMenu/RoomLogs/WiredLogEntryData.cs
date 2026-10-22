using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu.RoomLogs;

/// <summary>A line of the room's wired log (WiredLogEntry).</summary>
[WireStruct]
public sealed partial record WiredLogEntryData(long Id, byte Level, byte Source, string Message, long Timestamp, string TimestampText);
