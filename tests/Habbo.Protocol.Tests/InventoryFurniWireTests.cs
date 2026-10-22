using System.Buffers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Protocol.Tests;

/// <summary>The inventory entry the client turns into a FurnitureItem: the strip id first, the item's real id third,
/// which the client keeps as FurnitureItem.ref.</summary>
public class InventoryFurniWireTests
{
    private static InventoryItemData Item(int itemId, int roomId) => new(
        ItemId: itemId, ClassId: 26, IsWallItem: false, Data: FurniDataView.Plain, RoomId: roomId,
        Category: 0, Recyclable: true, Tradeable: true, Groupable: true, Sellable: true,
        SecondsToExpiration: -1, HasRentPeriodStarted: false, SlotId: "", Extra: 0);

    private static int ReadRef(InventoryItemData item)
    {
        var buffer = new ArrayBufferWriter<byte>();
        InventoryFurniData.From(item).Write(new MessageWriter(buffer));

        var reader = new MessageReader(buffer.WrittenMemory);
        reader.ReadInt();       // itemId
        reader.ReadString();    // item type
        return reader.ReadInt(); // roomItemId, which the client reads as ref
    }

    [Fact]
    public void AnInventoryItemCarriesItsRealIdAsRef()
    {
        // The client hands ref back as the item's id for marketplace offers and pet products, and checks it is not
        // negative before it cancels the mover.
        Assert.Equal(41, ReadRef(Item(itemId: 41, roomId: -1)));
    }

    [Fact]
    public void TheStripIdIsTheIdNegated()
    {
        // Field one keys the group, the removal, the unseen set and the trade locks, and is the id the client sends
        // back to place, trade, recycle or craft with the item: placing, it compares the placed object's id, negated,
        // to the ref.
        var buffer = new ArrayBufferWriter<byte>();
        InventoryFurniData.From(Item(itemId: 41, roomId: -1)).Write(new MessageWriter(buffer));

        Assert.Equal(-41, new MessageReader(buffer.WrittenMemory).ReadInt());
    }

    [Fact]
    public void AnInventoryItemMatchesHabbosBytes()
    {
        // Habbo.com handing back a Pineapple Plant (item 923650708, class 160) picked up from a room, 2026-10-10.
        var item = Item(itemId: 923650708, roomId: -1) with { ClassId = 160, Category = 1 };
        var buffer = new ArrayBufferWriter<byte>();
        InventoryFurniData.From(item).Write(new MessageWriter(buffer));

        Assert.Equal("C8F2356C000153370DCA94000000A00000000100000000000001010101FFFFFFFF00FFFFFFFF000000000000",
            Convert.ToHexString(buffer.WrittenSpan));
    }
}
