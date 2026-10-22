using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.CallForHelp;

/// <summary>SanctionStatusData</summary>
[WireStruct]
public sealed partial record SanctionStatusData(SanctionTypeData Current, string Description, bool OnProbation, int ProbationDaysLeft, SanctionTypeData Next);
