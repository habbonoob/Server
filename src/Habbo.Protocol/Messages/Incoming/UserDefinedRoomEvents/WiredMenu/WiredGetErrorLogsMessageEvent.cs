using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>1540 WiredGetErrorLogs — the monitor asks for the room's wired errors.</summary>
[Incoming(ClientMessages.WiredGetErrorLogsMessageEvent)]
public sealed partial record WiredGetErrorLogsMessageEvent;
