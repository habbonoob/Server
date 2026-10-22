using Habbo.Domain.Furniture;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Silo.Items;

/// <summary>The one place an owned item becomes its wire view. Both the inventory listing and
/// the room's pick-up path send the same item, so they have to describe it identically — a
/// disagreement here shows up as an item that changes shape when it leaves a room.</summary>
public static class InventoryProjection
{
    /// <summary>The client reports an unplaced item as being in no room.</summary>
    private const int NotInARoom = -1;

    public static InventoryItemData From(UserItem item, ItemDefinition definition, FurniDataView data) => new(
        item.Id,
        definition.ClassId,
        definition.Type == ItemType.Wall,
        data,
        item.RoomId ?? NotInARoom,
        FurniCategory.Of(definition),
        item.Recyclable,
        item.Tradeable,
        IsGroupable(definition, data),
        item.Sellable,
        item.SecondsToExpiration,
        item.HasRentPeriodStarted,
        item.SlotId,
        item.Extra);

    /// <summary>Whether this item may merge into a stack. The client keys a stack on the item
    /// type alone and shows one tile for the whole group, so an item carrying its own state
    /// must stay on its own tile or that state becomes invisible behind a count.</summary>
    private static bool IsGroupable(ItemDefinition definition, FurniDataView data) =>
        definition.Groupable && data.IsPlain;
}
