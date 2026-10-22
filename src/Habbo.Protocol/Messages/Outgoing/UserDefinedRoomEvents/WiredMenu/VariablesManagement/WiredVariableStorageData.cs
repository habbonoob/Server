using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu.VariablesManagement;

/// <summary>A stored variable value (WiredVariableStorageParameter), with its times as numbers and as text.</summary>
[WireStruct]
public sealed partial record WiredVariableStorageData(int Value, long CreatedAt, string CreatedAtText, long UpdatedAt, string UpdatedAtText);
