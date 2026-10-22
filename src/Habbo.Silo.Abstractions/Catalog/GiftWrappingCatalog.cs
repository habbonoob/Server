using Orleans;

namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>The wraps a gift may come in, by furni class id, as the client's dialog offers them.</summary>
[GenerateSerializer]
public sealed partial record GiftWrappingCatalog(
    [property: Id(0)] bool Enabled,
    [property: Id(1)] int Price,
    [property: Id(2)] List<int> WrapClassIds,
    [property: Id(3)] List<int> BoxTypes,
    [property: Id(4)] List<int> RibbonTypes,
    [property: Id(5)] List<int> DefaultClassIds);
