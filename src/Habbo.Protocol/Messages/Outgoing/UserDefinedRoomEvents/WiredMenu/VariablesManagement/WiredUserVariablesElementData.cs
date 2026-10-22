using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu.VariablesManagement;

/// <summary>A user holding a permanent variable, and its value (WiredUserVariablesElement).</summary>
[WireStruct]
public sealed partial record WiredUserVariablesElementData(int EntityType, int EntityId, string EntityName, WiredVariableStorageData Storage);
