using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.CallForHelp;

/// <summary>SanctionTypeData</summary>
[WireStruct]
public sealed partial record SanctionTypeData(string Name, int LengthInHours, int ProbationDays);
