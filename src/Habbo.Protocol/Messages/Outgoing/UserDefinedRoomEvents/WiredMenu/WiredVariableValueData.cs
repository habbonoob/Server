using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

/// <summary>A variable an object holds, with its value.</summary>
[WireStruct]
public sealed partial record WiredVariableValueData(string VariableId, int Value);
