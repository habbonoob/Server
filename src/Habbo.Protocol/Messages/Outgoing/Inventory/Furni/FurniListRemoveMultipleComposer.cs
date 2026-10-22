using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Furni;

/// <summary>FurniListRemoveMultipleComposer — items gone from the inventory</summary>
[Message(ServerMessages.FurniListRemoveMultipleComposer)]
public sealed partial record FurniListRemoveMultipleComposer(IReadOnlyList<int> StripIds)
{
    /// <summary>These items gone from the inventory.</summary>
    public static FurniListRemoveMultipleComposer Items(IEnumerable<int> itemIds) => new(itemIds.Select(InventoryStripId.Of).ToList());
}
