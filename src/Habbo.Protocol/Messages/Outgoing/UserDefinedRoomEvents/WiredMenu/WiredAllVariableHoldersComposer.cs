using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

/// <summary>986 WiredAllVariableHoldersComposer — everyone holding a variable. The client reads past
/// the leading int; it carries the variable's target.</summary>
[Message(ServerMessages.WiredAllVariableHoldersComposer)]
public sealed partial record WiredAllVariableHoldersComposer(int Target, WiredVariableData Variable, IReadOnlyList<WiredVariableHolderData> Holders);
