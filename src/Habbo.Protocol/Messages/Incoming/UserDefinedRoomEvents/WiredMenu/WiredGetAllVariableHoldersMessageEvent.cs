using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>3999 WiredGetAllVariableHolders — who holds a variable, for the overview tab.</summary>
[Incoming(ClientMessages.WiredGetAllVariableHoldersMessageEvent)]
public sealed partial record WiredGetAllVariableHoldersMessageEvent(string VariableId);
