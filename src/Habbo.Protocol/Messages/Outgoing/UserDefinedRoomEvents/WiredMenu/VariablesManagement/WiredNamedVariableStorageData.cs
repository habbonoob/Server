using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu.VariablesManagement;

/// <summary>A stored variable value with the variable it is of.</summary>
[WireStruct]
public sealed partial record WiredNamedVariableStorageData(string VariableId, int Value, long CreatedAt, string CreatedAtText, long UpdatedAt, string UpdatedAtText);
