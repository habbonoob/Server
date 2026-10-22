using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu.RoomLogs;

/// <summary>1692 WiredGetRoomLogs — a page of the room's wired log, filtered by level and source
/// (-1 for any) and by text.</summary>
[Incoming(ClientMessages.WiredGetRoomLogsEvent)]
public sealed partial record WiredGetRoomLogsEvent(int Page, int PageSize, int LevelFilter, int SourceFilter, string Query);
