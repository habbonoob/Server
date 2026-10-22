using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>3608 WiredClearErrorLogs — the monitor's "clear all".</summary>
[Incoming(ClientMessages.WiredClearErrorLogsMessageEvent)]
public sealed partial record WiredClearErrorLogsMessageEvent;
