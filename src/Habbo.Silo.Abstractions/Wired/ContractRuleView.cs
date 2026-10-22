using Orleans;

namespace Habbo.Silo.Abstractions.Wired;

/// <summary>A contract rule as the dialog sent it: all of its elements together.</summary>
[GenerateSerializer]
public sealed partial record ContractRuleView([property: Id(0)] IReadOnlyList<ContractNodeView> Nodes);
