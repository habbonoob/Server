using Habbo.Domain.Furniture;
using Habbo.Domain.Users;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Tests;

public class RoomStoreTests
{
    // 8x6 open floor, door at (0,1) on the left-edge notch.
    private static RoomModel Model() => RoomModelParser.Parse(
        "x0000000" + "\r" +
        "00000000" + "\r" +
        "x0000000" + "\r" +
        "x0000000" + "\r" +
        "x0000000" + "\r" +
        "x0000000",
        doorX: 0, doorY: 1);

    private static readonly ItemDefinition Chair = new()
    {
        Id = 26, ClassName = "chair_silo", XDim = 1, YDim = 1, StackHeight = 0.8, CanSit = true,
    };

    private static readonly ItemDefinition Sofa = new()
    {
        Id = 28, ClassName = "sofa_silo", XDim = 2, YDim = 1, StackHeight = 0.9, CanSit = true,
    };

    private static readonly ItemDefinition Rug = new()
    {
        Id = 59, ClassName = "carpet_standard", XDim = 2, YDim = 2, StackHeight = 0.0,
        CanWalk = true, CanStackOn = true,
    };

    private static readonly ItemDefinition Bed = new()
    {
        Id = 62, ClassName = "bed_silo_one", XDim = 1, YDim = 2, StackHeight = 1.0, CanLay = true,
    };

    private static readonly ItemDefinition Minibar = new()
    {
        Id = 63, ClassName = "bar_armas", XDim = 1, YDim = 1, StackHeight = 1.0,
    };

    private static readonly ItemDefinition Table = new()
    {
        Id = 60, ClassName = "table_silo", XDim = 1, YDim = 1, StackHeight = 0.9, CanStackOn = true,
    };

    private static readonly ItemDefinition Coin = new()
    {
        Id = 61, ClassName = "CF_1_coin_bronze", XDim = 1, YDim = 1, StackHeight = 0.16, CanStackOn = true,
    };

    private static int _nextItemId = 1;

    [Theory]
    [InlineData(0, true)]
    [InlineData(6, true)]
    [InlineData(1, false)]
    [InlineData(-2, false)]
    [InlineData(8, false)]
    [InlineData(9999, false)]
    public void WithoutImportedDirectionsOnlyTheSquareRotationsAreAllowed(int rotation, bool allowed) =>
        Assert.Equal(allowed, Chair.AllowsDirection(rotation));

    private static UserItem Item(ItemDefinition definition) =>
        new() { Id = _nextItemId++, DefinitionId = definition.Id, Definition = definition };

    private static PlayerData Player(string name) => new(
        Guid.NewGuid(), 1, name, "hd-180-1", Gender.Male, "", 0, SecurityLevel.AboveNone, "", false, 0, 3, 3, true, false, false,
        ClubLevel.None, false, 0, false, DateTimeOffset.UtcNow, PlayerData.NoHomeRoom, 0, false, "");

    private static readonly FurniInteractionRegistry Interactions = new();

    private static readonly Func<int, int, bool> NoAvatars = (_, _) => false;

    [Fact]
    public void TheTileIndexAgreesWithEveryItemsFootprintThroughPlacingMovingAndPickingUp()
    {
        var store = new RoomItemStore(Model(), Interactions);
        var random = new Random(1234);
        ItemDefinition[] kinds = [Chair, Sofa, Rug, Bed, Minibar, Table, Coin];
        for (var step = 0; step < 500; step++)
        {
            var placed = store.Items.ToList();
            var (x, y, rotation) = (random.Next(8), random.Next(6), random.Next(4) * 2);
            switch (random.Next(3))
            {
                case 0 or 1 when placed.Count < 20:
                    store.Add(Item(kinds[random.Next(kinds.Length)]), FurniDataView.Plain, x, y, rotation);
                    break;
                case 1 when placed.Count > 0:
                    store.MoveTo(placed[random.Next(placed.Count)], x, y, rotation);
                    break;
                default:
                    if (placed.Count > 0)
                        store.Remove(placed[random.Next(placed.Count)].Item.Id);
                    break;
            }

            for (var tx = 0; tx < 8; tx++)
            {
                for (var ty = 0; ty < 6; ty++)
                {
                    var expected = store.Items.Where(i => RoomItemStore.Footprint(i).Contains((tx, ty))).Select(i => i.Item.Id).Order();
                    Assert.Equal(expected, store.FloorItemsOn(tx, ty).Select(i => i.Item.Id).Order());
                }
            }
        }
    }

    [Fact]
    public void FootprintSwapsDimensionsWhenRotated()
    {
        var store = new RoomItemStore(Model(), Interactions);
        var sofa = store.Add(Item(Sofa), FurniDataView.Plain, 2, 2, rotation: 0);
        Assert.Equal([(2, 2), (3, 2)], RoomItemStore.Footprint(sofa).ToList());

        store.MoveTo(sofa, 2, 2, rotation: 2);
        Assert.Equal([(2, 2), (2, 3)], RoomItemStore.Footprint(sofa).ToList());
    }

    [Fact]
    public void PlacementRejectsDoorOverlapVoidAndAvatars()
    {
        var store = new RoomItemStore(Model(), Interactions);
        store.Add(Item(Chair), FurniDataView.Plain, 3, 3, rotation: 0);

        Assert.True(store.CanPlaceFloorItem(999, Chair, 4, 4, 0, NoAvatars));
        Assert.False(store.CanPlaceFloorItem(999, Chair, 0, 1, 0, NoAvatars));   // door
        Assert.False(store.CanPlaceFloorItem(999, Chair, 0, 0, 0, NoAvatars));   // void
        Assert.False(store.CanPlaceFloorItem(999, Chair, 3, 3, 0, NoAvatars));   // occupied by the chair
        Assert.False(store.CanPlaceFloorItem(999, Sofa, 7, 2, 0, NoAvatars));    // runs off the edge
        Assert.False(store.CanPlaceFloorItem(999, Chair, 5, 5, 0, (x, y) => (x, y) == (5, 5)));
    }

    [Fact]
    public void AnAvatarOnlyBlocksTilesTheItemDoesNotCoverAlready()
    {
        var store = new RoomItemStore(Model(), Interactions);
        var chair = store.Add(Item(Chair), FurniDataView.Plain, 3, 3, rotation: 0);
        var sofa = store.Add(Item(Sofa), FurniDataView.Plain, 3, 5, rotation: 0);
        Func<int, int, bool> avatars = (x, y) => (x, y) is (3, 3) or (4, 5) or (6, 1);

        Assert.True(store.CanPlaceFloorItem(chair.Item.Id, Chair, 3, 3, 2, avatars));    // rotated under its sitter
        Assert.True(store.CanPlaceFloorItem(sofa.Item.Id, Sofa, 4, 5, 0, avatars));      // shifted, still under its sitter
        Assert.False(store.CanPlaceFloorItem(sofa.Item.Id, Sofa, 5, 1, 0, avatars));     // onto someone it does not seat
    }

    [Fact]
    public void AnItemMayBeMovedOntoItsOwnTiles()
    {
        var store = new RoomItemStore(Model(), Interactions);
        var sofa = store.Add(Item(Sofa), FurniDataView.Plain, 2, 2, rotation: 0);

        // Rotating in place overlaps its own footprint — must not count as a collision.
        Assert.True(store.CanPlaceFloorItem(sofa.Item.Id, Sofa, 2, 2, 2, NoAvatars));
    }

    [Fact]
    public void WalkingAndStackingAreSeparateQuestions()
    {
        var store = new RoomItemStore(Model(), Interactions);
        store.Add(Item(Rug), FurniDataView.Plain, 2, 2, rotation: 0);
        store.Add(Item(Chair), FurniDataView.Plain, 5, 2, rotation: 0);

        // A rug can be walked over and carries furniture; a chair is walked over too, and
        // carries nothing.
        Assert.False(store.BlocksWalking(2, 2));
        Assert.False(store.StackingBlockedAt(2, 2));
        Assert.False(store.BlocksWalking(5, 2));
        Assert.True(store.StackingBlockedAt(5, 2));
    }

    [Fact]
    public void OnlyWhatNobodyCanUseBlocksATile()
    {
        var store = new RoomItemStore(Model(), Interactions);
        store.Add(Item(Rug), FurniDataView.Plain, 2, 2, rotation: 0);
        store.Add(Item(Chair), FurniDataView.Plain, 5, 2, rotation: 0);
        store.Add(Item(Bed), FurniDataView.Plain, 4, 2, rotation: 0);
        store.Add(Item(Minibar), FurniDataView.Plain, 6, 2, rotation: 0);

        // Anything an avatar can stand, sit or lie on is walked straight through.
        Assert.False(store.BlocksWalking(2, 2));
        Assert.False(store.BlocksWalking(5, 2));
        Assert.False(store.BlocksWalking(4, 2));

        // A minibar is none of those, and an empty tile is never in the way.
        Assert.True(store.BlocksWalking(6, 2));
        Assert.False(store.BlocksWalking(1, 3));
    }

    [Fact]
    public void OnlyWhatCanBeStoodOnLiftsAnAvatar()
    {
        var store = new RoomItemStore(Model(), Interactions);
        store.Add(Item(Rug), FurniDataView.Plain, 2, 2, rotation: 0);
        store.Add(Item(Bed), FurniDataView.Plain, 4, 2, rotation: 0);
        store.Add(Item(Chair), FurniDataView.Plain, 5, 2, rotation: 0);

        // Habbo walks an avatar across a 1.8-high double bed at 0.0, and sits it on a 1.0-high
        // chair at 0.0 with the height in the status instead. Only standing on something lifts.
        Assert.Equal(0, store.AvatarHeightAt(4, 2));
        Assert.Equal(0, store.AvatarHeightAt(5, 2));
        Assert.Equal(Rug.StackHeight, store.AvatarHeightAt(2, 2));
    }

    [Fact]
    public void ItemsStackOnWhateverCarriesThem()
    {
        var store = new RoomItemStore(Model(), Interactions);
        var table = store.Add(Item(Table), FurniDataView.Plain, 3, 2, rotation: 0);

        Assert.Equal(0, table.Z);
        Assert.Equal(0.9, store.SurfaceAt(3, 2));

        // A coin on the table rests on its surface, and another coin rests on that one.
        Assert.True(store.CanPlaceFloorItem(0, Coin, 3, 2, rotation: 0, NoAvatars));
        var first = store.Add(Item(Coin), FurniDataView.Plain, 3, 2, rotation: 0);
        Assert.Equal(0.9, first.Z);

        var second = store.Add(Item(Coin), FurniDataView.Plain, 3, 2, rotation: 0);
        Assert.Equal(0.9 + 0.16, second.Z, 3);
        Assert.Equal(0.9 + 0.16 + 0.16, store.SurfaceAt(3, 2), 3);
    }

    [Fact]
    public void NothingStacksOnAnItemThatCarriesNothing()
    {
        var store = new RoomItemStore(Model(), Interactions);
        store.Add(Item(Chair), FurniDataView.Plain, 3, 2, rotation: 0);

        Assert.False(store.CanPlaceFloorItem(0, Coin, 3, 2, rotation: 0, NoAvatars));
    }

    [Fact]
    public void AnItemMustRestLevelAcrossEveryTileItCovers()
    {
        var store = new RoomItemStore(Model(), Interactions);
        store.Add(Item(Table), FurniDataView.Plain, 2, 2, rotation: 0);

        // The sofa would stand half on the table and half on the floor.
        Assert.False(store.CanPlaceFloorItem(0, Sofa, 2, 2, rotation: 0, NoAvatars));

        // On the table alone it fits, once the second tile is raised too.
        store.Add(Item(Table), FurniDataView.Plain, 3, 2, rotation: 0);
        Assert.True(store.CanPlaceFloorItem(0, Sofa, 2, 2, rotation: 0, NoAvatars));
    }

    [Fact]
    public void MovingAnItemDoesNotStackItOnItself()
    {
        var store = new RoomItemStore(Model(), Interactions);
        var coin = store.Add(Item(Coin), FurniDataView.Plain, 3, 2, rotation: 0);
        Assert.Equal(0, coin.Z);

        store.MoveTo(coin, 3, 2, rotation: 2);

        Assert.Equal(0, coin.Z);
    }

    [Fact]
    public void TheTileMapCarriesStackHeights()
    {
        var store = new RoomItemStore(Model(), Interactions);
        store.Add(Item(Table), FurniDataView.Plain, 3, 2, rotation: 0);
        store.Add(Item(Chair), FurniDataView.Plain, 5, 2, rotation: 0);

        var tiles = store.TileMap().ToDictionary(t => (t.X, t.Y));

        Assert.Equal(0.9, tiles[(3, 2)].Height);
        Assert.False(tiles[(3, 2)].StackingBlocked);
        // a tile topped by what carries nothing is at the height that stands on, as Habbo sends it
        Assert.True(tiles[(5, 2)].StackingBlocked);
        Assert.Equal(0, tiles[(5, 2)].Height);
        Assert.Equal(0, tiles[(1, 1)].Height);

        // Void tiles are left out entirely.
        Assert.False(tiles.ContainsKey((0, 0)));
    }

    [Fact]
    public void SeatHeightIsWhereTheSeatActuallyIs()
    {
        var store = new RoomItemStore(Model(), Interactions);
        store.Add(Item(Sofa), FurniDataView.Plain, 2, 2, rotation: 0);

        Assert.Equal(0.9, store.SeatHeightAt(3, 2));
        Assert.Null(store.SeatHeightAt(4, 2));

        // A chair standing on a table seats its occupant that much higher.
        store.Add(Item(Table), FurniDataView.Plain, 5, 3, rotation: 0);
        store.Add(Item(Chair), FurniDataView.Plain, 5, 3, rotation: 0);
        Assert.Equal(0.9 + 0.8, store.SeatHeightAt(5, 3)!.Value, 3);
    }

    [Fact]
    public void WalkingOntoAChairSitsOnArrival()
    {
        var model = Model();
        var items = new RoomItemStore(model, Interactions);
        items.Add(Item(Chair), FurniDataView.Plain, 3, 1, rotation: 0);
        var entities = new RoomEntityStore(model, items);
        var entity = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("a"), doorDirection: 2);

        Assert.True(entities.Walk(entity, 3, 1));

        var statuses = new List<string>();
        for (var tick = 0; tick < 10 && (statuses.Count == 0 || statuses[^1].StartsWith("/mv")); tick++)
            statuses.AddRange(entities.Tick().Select(u => u.Status));

        Assert.Equal("/sit 0.8 0/", statuses[^1]);
        Assert.True(entity.Sitting);
    }

    [Fact]
    public void OneSentToABedsFootWalksToItsHead()
    {
        // Habbo: a click on a bed's foot tile walked to the head of that lane and lay there
        var model = Model();
        var items = new RoomItemStore(model, Interactions);
        items.Add(Item(Bed), FurniDataView.Plain, 3, 2, rotation: 0);
        var entities = new RoomEntityStore(model, items);
        var entity = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("a"), doorDirection: 2);

        Assert.True(entities.Walk(entity, 3, 3));

        Assert.Equal((3, 2), entity.Goal);
        Assert.Equal((3, 2), entity.Path.Last());
    }

    [Fact]
    public void AWalkGoesRoundAChairItDoesNotEndOn()
    {
        // Habbo: sent past a bed, the avatar walked round it; a seat or bed only ends a walk
        var model = Model();
        var items = new RoomItemStore(model, Interactions);
        items.Add(Item(Chair), FurniDataView.Plain, 3, 1, rotation: 0);
        var entities = new RoomEntityStore(model, items);
        var entity = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("a"), doorDirection: 2);

        Assert.True(entities.Walk(entity, 5, 1));

        Assert.DoesNotContain((3, 1), entity.Path);
        Assert.Equal((5, 1), entity.Path.Last());
    }

    [Fact]
    public void ARollersRiderIsToldWhereTheyStandOnTheTwoTicksAfter()
    {
        // Habbo: "/flatctrl 4///" the tick after the slide (an empty status besides), "/flatctrl 4//" the next
        var model = Model();
        var items = new RoomItemStore(model, Interactions);
        var entities = new RoomEntityStore(model, items);
        var entity = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("a"), doorDirection: 2);
        entities.Carry(entity, 2, 2);

        entities.Roll(entity, 2, 1);

        Assert.Equal(["///"], entities.Tick().Select(u => u.Status));
        Assert.Equal(["//"], entities.Tick().Select(u => u.Status));
        Assert.Empty(entities.Tick());
        Assert.Equal((2, 1), (entity.X, entity.Y));
    }

    [Fact]
    public void ARiderSittingOnTheFloorSlidesAtTheHeightItIsDrawnAt()
    {
        // Habbo, :sit on a roller 0.45 high: "sit 0.55 1", and the slide carries it from 1.0 to 1.0 rather than 0.45,
        // so it doesn't sink for the slide; it stays sitting, told so the tick after with the empty status besides
        var model = Model();
        var items = new RoomItemStore(model, Interactions);
        var entities = new RoomEntityStore(model, items);
        var entity = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("a"), doorDirection: 2);
        entities.Carry(entity, 2, 2);
        Assert.Equal(entity.Z, entities.DrawnHeight(entity));

        Assert.NotNull(entities.SitHere(entity));
        Assert.Equal(entity.Z + 0.55, entities.DrawnHeight(entity), precision: 6);

        entities.Roll(entity, 2, 1);

        Assert.True(entity.Sitting);
        Assert.Equal(entity.Z + 0.55, entities.DrawnHeight(entity), precision: 6);
        Assert.Equal(["//sit 0.55 1/"], entities.Tick().Select(u => u.Status));
    }

    [Fact]
    public void AWalkAskedForBeforeTheTickAfterASlideIsDropped()
    {
        // Habbo: a click between the slide and the tick after it never walks; one after that tick walks on the next
        var model = Model();
        var items = new RoomItemStore(model, Interactions);
        var entities = new RoomEntityStore(model, items);
        var entity = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("a"), doorDirection: 2);
        entities.Carry(entity, 2, 2);
        entities.Roll(entity, 2, 1);

        Assert.True(entities.Walk(entity, 2, 3));

        Assert.Equal(["///"], entities.Tick().Select(u => u.Status));
        Assert.Equal(["//"], entities.Tick().Select(u => u.Status));
        Assert.Equal((2, 1), (entity.X, entity.Y));

        Assert.True(entities.Walk(entity, 2, 3));
        Assert.StartsWith("/mv 2,2,", Assert.Single(entities.Tick()).Status);
    }

    [Fact]
    public void WalkingNeverRoutesThroughBlockingFurniture()
    {
        var model = Model();
        var items = new RoomItemStore(model, Interactions);
        // A wall of minibars at x=3 except the bottom row forces a detour.
        for (var y = 0; y < 5; y++)
            items.Add(Item(Minibar), FurniDataView.Plain, 3, y, rotation: 0);
        var entities = new RoomEntityStore(model, items);
        var entity = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("a"), doorDirection: 2);

        Assert.True(entities.Walk(entity, 6, 1));
        var visited = new List<(int, int)>();
        for (var tick = 0; tick < 30 && entity.Walking; tick++)
        {
            entities.Tick();
            visited.Add((entity.X, entity.Y));
        }

        Assert.Equal((6, 1), (entity.X, entity.Y));
        Assert.DoesNotContain(visited, tile => tile.Item1 == 3 && tile.Item2 < 5);
    }

    private static void Walk(RoomEntityStore entities, RoomEntityStore.RoomEntity entity, int x, int y)
    {
        Assert.True(entities.Walk(entity, x, y));
        for (var tick = 0; tick < 30 && entity.Walking; tick++)
            entities.Tick();
    }

    [Fact]
    public void WalkingOntoSomebodyStopsBesideThem()
    {
        var model = Model();
        var entities = new RoomEntityStore(model, new RoomItemStore(model, Interactions));
        var first = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("a"), doorDirection: 2);
        var second = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("b"), doorDirection: 2);

        // Walk the first one off the door so the two do not start out sharing a tile.
        Walk(entities, first, 5, 3);
        Assert.Equal((5, 3), (first.X, first.Y));

        // The walk still happens — it just ends on the nearest tile it can reach instead.
        Walk(entities, second, 5, 3);
        Assert.NotEqual((5, 3), (second.X, second.Y));
        Assert.Equal(1, Math.Max(Math.Abs(second.X - 5), Math.Abs(second.Y - 3)));
    }

    [Fact]
    public void TwoWalkingForTheSameTileAreNotBothStoppedFromTrying()
    {
        var model = Model();
        var entities = new RoomEntityStore(model, new RoomItemStore(model, Interactions));
        var near = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("a"), doorDirection: 2);
        var far = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("b"), doorDirection: 2);
        Walk(entities, far, 7, 5);

        // Nothing is reserved, so both walks start; the shorter route wins the tile and the
        // other settles next to it.
        Assert.True(entities.Walk(far, 2, 1));
        Assert.True(entities.Walk(near, 2, 1));

        for (var tick = 0; tick < 30 && (near.Walking || far.Walking); tick++)
            entities.Tick();

        Assert.Equal((2, 1), (near.X, near.Y));
        Assert.NotEqual((near.X, near.Y), (far.X, far.Y));
        Assert.False(far.Walking);
    }

    [Fact]
    public void FurnitureDroppedInFrontOfAWalkIsWalkedThrough()
    {
        var model = Model();
        var items = new RoomItemStore(model, Interactions);
        var entities = new RoomEntityStore(model, items);
        var entity = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("a"), doorDirection: 2);

        Assert.True(entities.Walk(entity, 6, 1));
        entities.Tick();

        // Drop a table on the tile it is about to step onto.
        var ahead = entity.Path.Peek();
        items.Add(Item(Table), FurniDataView.Plain, ahead.X, ahead.Y, rotation: 0);
        entities.Tick();

        // It steps straight onto the tile rather than routing around, and stays at floor level
        // rather than climbing the table.
        Assert.Equal(ahead, (entity.X, entity.Y));
        Assert.Equal(0, entity.Z);
    }

    [Fact]
    public void StandingUpOnlyEmitsWhenSitting()
    {
        var model = Model();
        var items = new RoomItemStore(model, Interactions);
        var entities = new RoomEntityStore(model, items);
        var entity = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("a"), doorDirection: 2);

        Assert.Null(entities.ChangePosture(entity, 0));   // not sitting: nothing to stand from
        Assert.Null(entities.ChangePosture(entity, 1));   // no seat under the door tile
    }

    [Fact]
    public void ReenteringRebindsTheSameEntity()
    {
        var model = Model();
        var entities = new RoomEntityStore(model, new RoomItemStore(model, Interactions));
        var playerId = Guid.NewGuid();

        var first = entities.Enter(playerId, Guid.NewGuid(), Player("a"), doorDirection: 2);
        entities.Walk(first, 5, 3);
        entities.Tick();

        var secondSession = Guid.NewGuid();
        var second = entities.Enter(playerId, secondSession, Player("a"), doorDirection: 2);

        Assert.Same(first, second);
        Assert.Equal(secondSession, second.SessionGuid);
        Assert.Equal((0, 1), (second.X, second.Y));
        Assert.False(second.Walking);
        Assert.Equal(1, entities.Count);
    }

    [Fact]
    public void TickBatchesEveryMovingEntity()
    {
        var model = Model();
        var entities = new RoomEntityStore(model, new RoomItemStore(model, Interactions));
        var a = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("a"), doorDirection: 2);
        entities.Walk(a, 4, 1);
        for (var tick = 0; tick < 6; tick++)
            entities.Tick(); // a walks clear of the door's only exit tile

        var b = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player("b"), doorDirection: 2);
        entities.Walk(a, 4, 4);
        entities.Walk(b, 6, 1);

        Assert.Equal(2, entities.Tick().Count);
    }
}
