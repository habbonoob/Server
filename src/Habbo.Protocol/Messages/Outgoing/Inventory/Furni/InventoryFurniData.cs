using Habbo.Domain.Furniture;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Inventory;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Furni;

/// <summary>One FurniData entry as the inventory packets carry it. A wall item stops after
/// the flat id; a floor item carries a slot and an extra, so those two are optional.</summary>
[WireStruct]
public sealed partial record InventoryFurniData(
    /// <summary>The strip id: see <see cref="InventoryStripId"/>.</summary>
    int ItemId,
    string ItemType,

    /// <summary>The client's FurnitureItem.ref: the item's own id.</summary>
    int RoomItemId,
    int ClassId,
    int Category,
    FurniStuffData StuffData,
    bool Recyclable,
    bool Tradeable,
    bool Groupable,
    bool Sellable,
    int SecondsToExpiration,
    bool HasRentPeriodStarted,
    int FlatId,
    string? SlotId,
    int? Extra)
{
    /// <summary>The client's item type discriminator: "I" for a wall item, "S" for a floor item.</summary>
    private const string WallItemType = "I";
    private const string FloorItemType = "S";

    public static InventoryFurniData From(InventoryItemData item) => new(
        InventoryStripId.Of(item.ItemId),
        item.IsWallItem ? WallItemType : FloorItemType,
        item.ItemId,
        item.ClassId,
        item.Category,
        FurniStuffData.From(item.Data),
        item.Recyclable,
        item.Tradeable,
        item.Groupable,
        item.Sellable,
        item.SecondsToExpiration,
        item.HasRentPeriodStarted,
        item.RoomId,
        item.IsWallItem ? null : item.SlotId,
        item.IsWallItem ? null : item.Extra);
}
