using Orleans;

namespace Habbo.Silo.Abstractions.Wired;

/// <summary>An element of a contract rule as the dialog sent it: credits (type 0), or items of one kind (type 1).</summary>
[GenerateSerializer]
public sealed partial record ContractNodeView(
    [property: Id(0)] byte Type,
    [property: Id(1)] int Amount,
    [property: Id(2)] bool IsWallItem,
    [property: Id(3)] int TypeId,
    [property: Id(4)] string PosterId);
