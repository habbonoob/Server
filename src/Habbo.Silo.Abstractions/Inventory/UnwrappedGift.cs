using Orleans;

namespace Habbo.Silo.Abstractions.Inventory;

/// <summary>What opening a present handed over: the new items, the first of them described for the
/// client's "you got" window.</summary>
[GenerateSerializer]
public sealed partial record UnwrappedGift(
    [property: Id(0)] List<int> ItemIds,
    [property: Id(1)] string ItemType,
    [property: Id(2)] int ClassId,
    [property: Id(3)] string ProductCode,
    [property: Id(4)] bool FirstIsFloorItem);
