using Orleans;

namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>A gift's wrapping as the buyer chose it.</summary>
[GenerateSerializer]
public sealed partial record GiftWrapping(
    [property: Id(0)] string ReceiverName,
    [property: Id(1)] string Message,
    [property: Id(2)] int SpriteId,
    [property: Id(3)] int BoxId,
    [property: Id(4)] int RibbonId,
    [property: Id(5)] bool ShowPurchaserName);
