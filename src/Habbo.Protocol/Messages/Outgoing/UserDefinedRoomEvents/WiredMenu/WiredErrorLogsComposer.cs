using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

/// <summary>345 WiredErrorLogsComposer — the room's wired errors, for the monitor.</summary>
[Message(ServerMessages.WiredErrorLogsComposer)]
public sealed partial record WiredErrorLogsComposer(IReadOnlyList<WiredErrorData> Errors);
