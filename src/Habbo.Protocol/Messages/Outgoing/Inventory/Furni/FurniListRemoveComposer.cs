using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Furni;

/// <summary>1372 FurniListRemoveComposer — the inventory strip id.</summary>
[Message(ServerMessages.FurniListRemoveComposer)]
public sealed partial record FurniListRemoveComposer(int StripId)
{
    /// <summary>This item gone from the inventory.</summary>
    public static FurniListRemoveComposer Item(int itemId) => new(InventoryStripId.Of(itemId));
}
