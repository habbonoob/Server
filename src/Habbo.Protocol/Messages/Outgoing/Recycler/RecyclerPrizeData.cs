using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Recycler;

/// <summary>RecyclerPrizeData — one prize: a piece of furniture</summary>
[WireStruct]
public sealed partial record RecyclerPrizeData(string ProductCode, int Count, [property: WireWhen("IsSingle")] string? ProductItemType, [property: WireWhen("IsSingle")] int? ProductItemTypeId)
{
    /// <summary>A prize of one item carries its type; a deal of several would list them instead.</summary>
    public bool IsSingle => Count <= 1;
}
