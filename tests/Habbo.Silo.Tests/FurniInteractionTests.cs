using Habbo.Domain.Furniture;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Room.Furniture;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Tests;

public class FurniInteractionTests
{
    private static readonly FurniInteractionRegistry Interactions = new();

    private static RoomModel Model() => RoomModelParser.Parse(
        "x0000000" + "\r" +
        "00000000" + "\r" +
        "x0000000" + "\r" +
        "x0000000",
        doorX: 0, doorY: 1);

    private static ItemDefinition Definition(string logic, int[] states, string? interaction = null, string customParams = "") => new()
    {
        Id = 1,
        ClassName = "test_" + logic,
        Logic = logic,
        Interaction = interaction,
        CustomParams = customParams,
        States = states.Select(state => new ItemDefinitionState { State = state }).ToList(),
    };

    private static PlayerData Player() => new(
        Guid.NewGuid(), 1, "tester", "hd-180-1", Gender.Male, "", 0, SecurityLevel.AboveNone, "", false, 0, 3, 3, true, false, false,
        ClubLevel.None, false, 0, false, DateTimeOffset.UtcNow, PlayerData.NoHomeRoom, 0, false, "");

    /// <summary>A room with one item at (4,2) and one avatar at the door (0,1).</summary>
    private static (RoomItemStore.PlacedItem Item, RoomEntityStore.RoomEntity User, FakeRoom Room) Setup(
        ItemDefinition definition, string state = "")
    {
        var model = Model();
        var items = new RoomItemStore(model, Interactions);
        var placed = items.Add(
            new UserItem { Id = 7, Definition = definition, DefinitionId = definition.Id },
            FurniDataView.WithState(state), 4, 2, rotation: 0);
        var entities = new RoomEntityStore(model, items);
        var user = entities.Enter(Guid.NewGuid(), Guid.NewGuid(), Player(), doorDirection: 2);
        return (placed, user, new FakeRoom(items));
    }

    private static Task Use(RoomItemStore.PlacedItem item, RoomEntityStore.RoomEntity user, FakeRoom room, FurniAction action) =>
        item.Interaction.UseAsync(new FurniUse(item, user, action, 0, room));

    [Fact]
    public void BehaviourFollowsLogicUnlessTheServerSaysOtherwise()
    {
        Assert.IsType<MultiStateInteraction>(Interactions.For(Definition("furniture_multistate", [0, 1])));
        Assert.IsType<DiceInteraction>(Interactions.For(Definition("furniture_dice", [-1, 0, 1])));

        // A wired box is multistate to the client; to the server it is a wired box, whose use the
        // room answers by opening its dialog.
        Assert.IsType<WiredBoxInteraction>(Interactions.For(Definition("furniture_multistate", [0, 1], "wired_trigger")));
        Assert.IsType<BasicInteraction>(Interactions.For(Definition("furniture_not_implemented", [0])));
    }

    [Theory]
    [InlineData("vending_machine")]
    [InlineData("effect_vending_machine")]
    public async Task ADispenserWorksAMomentThenGivesOneOfWhatItLists(string interaction)
    {
        var definition = Definition("furniture_multistate", [0, 1], interaction);
        definition.VendingIds = [17, 18];
        var (item, user, room) = Setup(definition);

        await Use(item, user, room, FurniAction.Use);
        Assert.Equal("1", item.Data.State);
        Assert.Equal((0, 0), (user.HandItem, user.EffectId));

        await room.RunScheduledAsync();
        Assert.Equal("0", item.Data.State);
        var given = interaction == "vending_machine" ? user.HandItem : user.EffectId;
        Assert.Contains(given, definition.VendingIds);
        // the user went to its front to be served
        Assert.True(RoomFurnitureUse.Reaches(user, item));
    }

    [Fact]
    public async Task ADispenserThatListsNothingDoesNothing()
    {
        var (item, user, room) = Setup(Definition("furniture_multistate", [0, 1], "vending_machine"));

        await Use(item, user, room, FurniAction.Use);
        await room.RunScheduledAsync();

        Assert.Equal("", item.Data.State);
        Assert.Equal(0, user.HandItem);
    }

    [Fact]
    public async Task MultiStateStepsThroughItsStatesAndWraps()
    {
        var (item, user, room) = Setup(Definition("furniture_multistate", [0, 1, 3]));

        await Use(item, user, room, FurniAction.Use);
        Assert.Equal("1", item.Data.State);
        await Use(item, user, room, FurniAction.Use);
        Assert.Equal("3", item.Data.State);
        await Use(item, user, room, FurniAction.Use);
        Assert.Equal("0", item.Data.State);
        Assert.Equal(3, room.Updates);
    }

    [Fact]
    public async Task MultiStateWithOneStateDoesNothing()
    {
        var (item, user, room) = Setup(Definition("furniture_multistate", [0]));

        await Use(item, user, room, FurniAction.Use);

        Assert.Equal(0, room.Updates);
    }

    [Fact]
    public async Task DiceRollsThenLandsOnAFace()
    {
        var (item, user, room) = Setup(Definition("furniture_dice", [-1, 0, 1, 2, 3, 4, 5, 6, 20]));
        user.X = 3;
        user.Y = 2;

        await Use(item, user, room, FurniAction.ThrowDice);
        Assert.Equal([-1], room.DiceValues);
        Assert.True(item.IsBusy);

        // A second throw mid-roll is ignored.
        await Use(item, user, room, FurniAction.ThrowDice);
        Assert.Single(room.DiceValues);

        await room.RunScheduledAsync();
        Assert.InRange(room.DiceValues[^1], 1, 6);
        Assert.Equal(room.DiceValues[^1].ToString(), item.Data.State);
    }

    [Fact]
    public async Task DiceOutOfReachIsNotThrown()
    {
        var (item, user, room) = Setup(Definition("furniture_dice", [-1, 0, 1, 2, 3, 4, 5, 6]));

        await Use(item, user, room, FurniAction.ThrowDice);

        Assert.Empty(room.DiceValues);
    }

    [Fact]
    public async Task RandomStateUsesTheOutcomesItsParamsName()
    {
        var (item, user, room) = Setup(Definition("furniture_randomstate", [0, 1, 2], customParams: "states=8,delay=2000"));

        await Use(item, user, room, FurniAction.UseRandom);

        Assert.InRange(int.Parse(item.Data.State!), 1, 8);
        Assert.True(item.IsBusy);
    }

    [Fact]
    public async Task CreditFurniPaysItsOwnerAndIsDestroyed()
    {
        var definition = Definition("furniture_credit", [0]);
        definition.CreditValue = 1000;
        var (item, user, room) = Setup(definition);
        item.Item.OwnerId = user.Player.Id;

        await Use(item, user, room, FurniAction.Redeem);

        Assert.Equal([1000], room.CreditsPaid);
        Assert.Equal([item.Item.Id], room.Removed);
    }

    [Fact]
    public async Task CreditFurniIsNotExchangedByAPasserBy()
    {
        var definition = Definition("furniture_credit", [0]);
        definition.CreditValue = 1000;
        var (item, user, room) = Setup(definition);
        item.Item.OwnerId = Guid.NewGuid();

        await Use(item, user, room, FurniAction.Redeem);

        Assert.Empty(room.CreditsPaid);
        Assert.Empty(room.Removed);
    }

    [Fact]
    public async Task AWiredCopyOfCreditFurniIsNotExchanged()
    {
        var definition = Definition("furniture_credit", [0]);
        definition.CreditValue = 1000;
        var (item, user, room) = Setup(definition);
        item.Item.OwnerId = user.Player.Id;
        var copy = room.Items.AddCopy(item, item.Data, 5, 2, z: null, rotation: 0)!;

        await Use(copy, user, room, FurniAction.Redeem);

        Assert.Empty(room.CreditsPaid);
        Assert.Empty(room.Removed);
    }

    [Fact]
    public async Task CreditFurniAlreadyGoneFromTheDatabaseIsNotPaidAgain()
    {
        var definition = Definition("furniture_credit", [0]);
        definition.CreditValue = 1000;
        var (item, user, room) = Setup(definition);
        item.Item.OwnerId = user.Player.Id;
        room.AlreadyGone = true;

        await Use(item, user, room, FurniAction.Redeem);

        Assert.Empty(room.CreditsPaid);
        Assert.Equal([item.Item.Id], room.Removed);
    }

    [Fact]
    public async Task CreditFurniWithoutAWorthIsLeftAlone()
    {
        var (item, user, room) = Setup(Definition("furniture_credit", [0]));
        item.Item.OwnerId = user.Player.Id;

        await Use(item, user, room, FurniAction.Redeem);

        Assert.Empty(room.CreditsPaid);
        Assert.Empty(room.Removed);
    }

    [Fact]
    public void PlacementRefusesARotationWithoutArtwork()
    {
        var definition = Definition(ItemDefinition.BasicLogic, [0]);
        definition.Directions = [new ItemDefinitionDirection { Direction = 2 }, new ItemDefinitionDirection { Direction = 4 }];
        var items = new RoomItemStore(Model(), Interactions);

        Assert.True(items.CanPlaceFloorItem(1, definition, 4, 2, rotation: 2, (_, _) => false));
        Assert.False(items.CanPlaceFloorItem(1, definition, 4, 2, rotation: 0, (_, _) => false));
    }

    /// <summary>Records what behaviours do instead of storing and broadcasting it.</summary>
    private sealed class FakeRoom(RoomItemStore items) : IFurniRoom
    {
        private readonly List<Func<Task>> _scheduled = [];

        public int Updates { get; private set; }
        public List<int> DiceValues { get; } = [];

        public Task UpdateAsync(RoomItemStore.PlacedItem item, FurniDataView data)
        {
            item.Data = data;
            Updates++;
            return Task.CompletedTask;
        }

        public Task SaveAsync(RoomItemStore.PlacedItem item, FurniDataView data)
        {
            item.Data = data;
            return Task.CompletedTask;
        }

        public List<int> Removed { get; } = [];
        public List<int> CreditsPaid { get; } = [];

        public RoomItemStore Items => items;

        /// <summary>Whether removing finds the item already gone from the database, as when redeemed elsewhere.</summary>
        public bool AlreadyGone { get; set; }

        public Task<bool> RemoveAsync(RoomItemStore.PlacedItem item)
        {
            Removed.Add(item.Item.Id);
            items.Remove(item.Item.Id);
            return Task.FromResult(!item.Temporary && !AlreadyGone);
        }

        public Task GiveCreditsAsync(RoomEntityStore.RoomEntity user, int credits)
        {
            CreditsPaid.Add(credits);
            return Task.CompletedTask;
        }

        public void Broadcast(IComposer composer)
        {
            if (composer is DiceValueMessageComposer dice)
                DiceValues.Add(dice.Value);
        }

        public void Schedule(TimeSpan delay, Func<Task> action) => _scheduled.Add(action);

        public void ShowData(RoomItemStore.PlacedItem item)
        {
        }

        public Task BatchedAsync(Func<Task> work) => work();

        public bool IsOccupied(RoomItemStore.PlacedItem item) => false;

        public void Tell(RoomEntityStore.RoomEntity user, IComposer composer)
        {
        }

        public async Task RunScheduledAsync()
        {
            foreach (var action in _scheduled.ToList())
                await action();
            _scheduled.Clear();
        }

        public bool IsPlaced(RoomItemStore.PlacedItem item) => ReferenceEquals(items.Find(item.Item.Id), item);

        public bool IsWithinReach(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item) =>
            RoomFurnitureUse.Reaches(user, item);

        public Task WalkThenAsync(RoomEntityStore.RoomEntity user, int x, int y, Func<Task> then)
        {
            user.X = x;
            user.Y = y;
            return then();
        }

        public void LookAt(RoomEntityStore.RoomEntity user, int x, int y)
        {
        }

        public void SetHandItem(RoomEntityStore.RoomEntity user, int handItem) => user.HandItem = handItem;


        public void WearEffect(RoomEntityStore.RoomEntity user, int effectId) => user.EffectId = effectId;

        public void DressInRoom(RoomEntityStore.RoomEntity user, string? outfitFigure) => user.RoomOutfit = outfitFigure;
    }
}
