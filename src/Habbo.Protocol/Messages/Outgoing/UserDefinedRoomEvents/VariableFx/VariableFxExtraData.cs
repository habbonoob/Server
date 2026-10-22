using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.VariableFx;

/// <summary>A setting of a variable display beyond its fixed ones: its icon, design, colour, level, and so on.</summary>
[WireStruct]
public sealed partial record VariableFxExtraData(string Key, string Value);
