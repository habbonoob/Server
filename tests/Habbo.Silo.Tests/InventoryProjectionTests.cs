using Habbo.Domain.Furniture;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Items;

namespace Habbo.Silo.Tests;

/// <summary>The client stacks inventory tiles by item type alone, so what we mark groupable
/// decides both whether copies merge and whether per-item state survives the merge.</summary>
public class InventoryProjectionTests
{
    private static ItemDefinition Definition(bool groupable = true) => new()
    {
        Id = 1,
        ClassId = 26,
        ClassName = "chair_silo",
        Type = ItemType.Floor,
        Groupable = groupable,
    };

    private static UserItem Item(int? roomId = null) => new() { Id = 1, DefinitionId = 1, RoomId = roomId };

    [Fact]
    public void OrdinaryFurnitureStacks()
    {
        Assert.True(InventoryProjection.From(Item(), Definition(), FurniDataView.Plain).Groupable);
    }

    [Fact]
    public void ADefinitionMarkedUngroupableDoesNotStack()
    {
        Assert.False(InventoryProjection.From(Item(), Definition(groupable: false), FurniDataView.Plain).Groupable);
    }

    [Fact]
    public void AnItemCarryingItsOwnStateDoesNotStack()
    {
        // A stack is keyed on the type and renders one tile, so merging items that differ
        // would hide whatever makes them different.
        Assert.False(InventoryProjection.From(Item(), Definition(), FurniDataView.WithState("3")).Groupable);
    }

    [Fact]
    public void ALimitedEditionDoesNotStack()
    {
        // Each one has its own number in the run, which a shared tile could not show.
        var limited = FurniDataView.Plain with { UniqueSerialNumber = 7, UniqueSeriesSize = 100 };
        Assert.False(InventoryProjection.From(Item(), Definition(), limited).Groupable);
    }

    [Fact]
    public void TheDataIsCarriedThroughUntouched()
    {
        var data = FurniDataView.WithState("2");
        Assert.Same(data, InventoryProjection.From(Item(), Definition(), data).Data);
    }

    [Fact]
    public void AnUnplacedItemReportsNoRoom()
    {
        Assert.Equal(-1, InventoryProjection.From(Item(), Definition(), FurniDataView.Plain).RoomId);
    }

    [Fact]
    public void APlacedItemReportsItsRoom()
    {
        Assert.Equal(7, InventoryProjection.From(Item(roomId: 7), Definition(), FurniDataView.Plain).RoomId);
    }
}
