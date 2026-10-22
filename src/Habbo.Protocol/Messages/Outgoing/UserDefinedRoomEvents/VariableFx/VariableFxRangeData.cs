using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.VariableFx;

/// <summary>The range a variable display shows its value in, for one holder.</summary>
[WireStruct]
public sealed partial record VariableFxRangeData(long Min, long Max);
