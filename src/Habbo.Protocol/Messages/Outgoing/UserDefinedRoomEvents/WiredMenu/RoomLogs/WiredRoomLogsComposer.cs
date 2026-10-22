using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu.RoomLogs;

/// <summary>54 WiredRoomLogsComposer — a page of the room's wired log (WiredLogPage): the totals, the
/// lines, then each filter it was asked with, each behind a bool saying whether it was.</summary>
[Message(ServerMessages.WiredRoomLogsComposer)]
public sealed partial record WiredRoomLogsComposer(
    int TotalEntries,
    int Page,
    int PageSize,
    IReadOnlyList<WiredLogEntryData> Entries,
    [property: WirePresence] byte? LevelFilter,
    [property: WirePresence] byte? SourceFilter,
    [property: WirePresence] string? Query);
