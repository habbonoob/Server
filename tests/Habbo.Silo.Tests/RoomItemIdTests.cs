using Habbo.Domain.Furniture;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Tests;

/// <summary>The ids a room gives its items: the database's, but for Builders Club furni, which the client tells apart by
/// an id in the club's range (habbo.com sent the first one put down in a room as 2147418112).</summary>
public class RoomItemIdTests
{
    private static readonly FurniInteractionRegistry Interactions = new();

    private static readonly ItemDefinition Chair = new()
    {
        Id = 26, ClassName = "chair_silo", XDim = 1, YDim = 1, StackHeight = 0.8, CanSit = true,
    };

    private static readonly ItemDefinition Poster = new()
    {
        Id = 4001, ClassName = "poster", Type = ItemType.Wall,
    };

    private static RoomItemStore Store() => new(RoomModelParser.Parse(
        "00000000" + "\r" +
        "00000000" + "\r" +
        "00000000",
        doorX: 0, doorY: 0), Interactions);

    private static UserItem Item(int id, bool buildersClub, ItemDefinition? definition = null, int x = 1) =>
        new()
        {
            Id = id, DefinitionId = (definition ?? Chair).Id, Definition = definition ?? Chair, BuildersClub = buildersClub,
            X = x, Y = 1, Z = 0, Rotation = 0, WallPosition = definition?.Type == ItemType.Wall ? ":w=1,1 l=0,0 r" : null,
        };

    [Fact]
    public void TheFirstBuildersClubIdIsTheOneHabboGaveAndTheClientTakesForBuildersClub()
    {
        Assert.Equal(0x7FFF0000, RoomItemStore.FirstBuildersClubId);
        Assert.Equal(RoomItemStore.FirstBuildersClubId - 1, RoomItemStore.LastTemporaryId);
    }

    [Fact]
    public void BuildersClubFurniTakesTheClubsIdsInTheOrderTheRoomLoadsIt()
    {
        var store = Store();
        store.Load([Item(10, buildersClub: false), Item(11, buildersClub: true, x: 2), Item(12, buildersClub: true, x: 3)],
            new Dictionary<int, FurniDataView>());

        Assert.Equal(10, store.FindStored(10)!.Id);
        Assert.Equal(RoomItemStore.FirstBuildersClubId, store.FindStored(11)!.Id);
        Assert.Equal(RoomItemStore.FirstBuildersClubId + 1, store.FindStored(12)!.Id);
    }

    [Fact]
    public void AnItemIsFoundByTheIdTheRoomGaveItAndByItsDatabaseId()
    {
        var store = Store();
        var borrowed = store.Add(Item(77, buildersClub: true), FurniDataView.Plain, 1, 1, 0);

        Assert.Same(borrowed, store.Find(borrowed.Id));
        Assert.Same(borrowed, store.FindStored(77));
        Assert.Null(store.Find(77));
        Assert.Equal(borrowed.Id, store.IdOfStored(77));
        Assert.Equal(77, store.StoredIdOf(borrowed.Id));
        // an id it does not know is passed through: any item but Builders Club furni goes by its database id
        Assert.Equal(5, store.IdOfStored(5));
        Assert.Equal(5, store.StoredIdOf(5));
    }

    [Fact]
    public void BuildersClubFurniPutDownLaterTakesTheNextIdAndOnePickedUpIsNotUsedAgainAtOnce()
    {
        var store = Store();
        var first = store.Add(Item(20, buildersClub: true), FurniDataView.Plain, 1, 1, 0);
        var wall = store.AddWall(Item(21, buildersClub: true, Poster), FurniDataView.Plain);
        Assert.True(store.Remove(first.Id));
        var third = store.Add(Item(22, buildersClub: true), FurniDataView.Plain, 1, 1, 0);

        Assert.Equal(RoomItemStore.FirstBuildersClubId, first.Id);
        Assert.Equal(RoomItemStore.FirstBuildersClubId + 1, wall.Id);
        Assert.Equal(RoomItemStore.FirstBuildersClubId + 2, third.Id);
        Assert.Null(store.Find(first.Id));
        Assert.Null(store.FindStored(20));
    }

    [Fact]
    public void AnItemPutInAgainKeepsTheIdTheRoomGaveIt()
    {
        var store = Store();
        var borrowed = store.Add(Item(30, buildersClub: true), FurniDataView.Plain, 1, 1, 0);
        var again = store.Add(Item(30, buildersClub: true), FurniDataView.Plain, 2, 1, 0);

        Assert.Equal(borrowed.Id, again.Id);
        Assert.Same(again, store.Find(borrowed.Id));
        Assert.Single(store.Items);
        Assert.Equal([again], store.FloorItemsOn(2, 1));
        Assert.Empty(store.FloorItemsOn(1, 1));
    }

    [Fact]
    public void TheClientIsToldOfBuildersClubFurniByItsRoomId()
    {
        var store = Store();
        var borrowed = store.Add(Item(40, buildersClub: true), FurniDataView.Plain, 1, 1, 0);
        var poster = store.AddWall(Item(41, buildersClub: true, Poster), FurniDataView.Plain);
        var owned = store.Add(Item(42, buildersClub: false), FurniDataView.Plain, 3, 1, 0);

        Assert.Equal(borrowed.Id, RoomItemStore.ToWireData(borrowed).ItemId);
        Assert.Equal(poster.Id, RoomItemStore.ToWireData(poster).ItemId);
        Assert.Equal(42, RoomItemStore.ToWireData(owned).ItemId);
        Assert.Equal([42, borrowed.Id], store.Project(ItemType.Floor).Select(item => item.ItemId).Order());
    }

    [Fact]
    public void AnItemIsLeftOutOfItsOwnStackByTheIdTheRoomGaveIt()
    {
        var store = Store();
        var borrowed = store.Add(Item(50, buildersClub: true), FurniDataView.Plain, 1, 1, 0);

        Assert.Equal(0.0, store.SurfaceAt(1, 1, borrowed.Id));
        Assert.True(store.CanPlaceFloorItem(borrowed.Id, Chair, 1, 1, 0, (_, _) => false));
    }

    [Fact]
    public void WiredCopiesKeepToTheTemporaryIds()
    {
        var store = Store();
        var borrowed = store.Add(Item(60, buildersClub: true), FurniDataView.Plain, 1, 1, 0);
        var copy = store.AddCopy(borrowed, borrowed.Data, 2, 1, z: null, rotation: 0)!;

        Assert.InRange(copy.Id, RoomItemStore.FirstTemporaryId, RoomItemStore.LastTemporaryId);
        Assert.Same(copy, store.Find(copy.Id));
    }
}
