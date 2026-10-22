using Habbo.Domain.Furniture;
using Habbo.Domain.Users;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Achievements;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Tests;

/// <summary>Battle Banzai and Freeze as measured on Habbo (docs/games.md).</summary>
public class RoomGameArenaTests
{
    private static readonly FurniInteractionRegistry Interactions = new();

    private const int Red = RoomGames.Red;
    private const int Blue = RoomGames.Blue;

    /// <summary>A room of games: its items, users, what it broadcast, and work scheduled for later, run on demand.</summary>
    private sealed class Room : IFurniRoom
    {
        private int _nextId = 1;
        private readonly List<(TimeSpan At, int Order, Func<Task> Action)> _scheduled = [];
        private TimeSpan _now;
        private int _order;

        public RoomItemStore Items { get; }
        public RoomEntityStore Entities { get; }
        public RoomGames Games { get; }
        public List<IComposer> Broadcasts { get; } = [];
        public List<(RoomEntityStore.RoomEntity User, string Achievement, int Amount)> Achievements { get; } = [];
        public List<string> OwnerAchievements { get; } = [];

        public Room(int width = 8, int height = 8)
        {
            var rows = Enumerable.Repeat(new string('0', width), height);
            var model = RoomModelParser.Parse(string.Join("\r", rows), doorX: 0, doorY: 0);
            Items = new RoomItemStore(model, Interactions);
            Entities = new RoomEntityStore(model, Items);
            Games = new RoomGames(Items, Entities, this, new NoEffects(), (user, effect) => user.EffectId = effect,
                new GameRoomActions(MoveAsync, Entities.HasAvatarAt, (user, x, y) => Entities.Warp(user, x, y),
                    (user, frozen) => user.Frozen = frozen,
                    (user, achievement, amount) => Achievements.Add((user, achievement, amount)),
                    (achievement, _) => OwnerAchievements.Add(achievement)));
        }

        public RoomItemStore.PlacedItem Furni(int x, int y, string className, string interaction, string customParams = "", string state = "0",
            bool canWalk = true, int width = 1)
        {
            var definition = new ItemDefinition
            {
                Id = className.GetHashCode(),
                ClassName = className,
                Interaction = interaction,
                CustomParams = customParams,
                XDim = width,
                CanWalk = canWalk,
                CanStackOn = true,
                StackHeight = 0,
            };
            return Items.Add(new UserItem { Id = _nextId++, Definition = definition, DefinitionId = definition.Id },
                FurniDataView.WithState(state), x, y, rotation: 0);
        }

        public RoomItemStore.PlacedItem Clock(int x, int y, string className = "bb_counter") =>
            Furni(x, y, className, GameClockInteraction.Name, "30", "30", canWalk: false);

        /// <summary>A Freeze counter: its start makes the game one of Freeze.</summary>
        public RoomItemStore.PlacedItem FreezeClock(int x, int y) => Clock(x, y, "es_counter");

        /// <summary>A rectangle of Banzai tiles.</summary>
        public void BanzaiArena(int x, int y, int width, int height)
        {
            for (var dx = 0; dx < width; dx++)
            for (var dy = 0; dy < height; dy++)
                Furni(x + dx, y + dy, "bb_patch1", BanzaiTileInteraction.Name);
        }

        /// <summary>A rectangle of Freeze tiles, with Habbo's rules.</summary>
        public void FreezeArena(int x, int y, int width, int height)
        {
            for (var dx = 0; dx < width; dx++)
            for (var dy = 0; dy < height; dy++)
                Furni(x + dx, y + dy, "es_tile", FreezeTileInteraction.Name, "12,12,12,12:49,50,51,52:10,-5:100:3");
        }

        public RoomEntityStore.RoomEntity User(int x, int y, int team = 0, int teamEffect = 40)
        {
            var player = new PlayerData(
                Guid.NewGuid(), _nextId++, $"player{_nextId}", "hd-180-1", Gender.Male, "", 0, SecurityLevel.AboveNone, "", false, 0, 3, 3, true,
                false, false, ClubLevel.None, false, 0, false, DateTimeOffset.UtcNow, PlayerData.NoHomeRoom, 0, false, "");
            var user = Entities.Enter(player.Id, Guid.NewGuid(), player, doorDirection: 2);
            Entities.Warp(user, x, y);
            if (team != 0)
                Games.JoinAsync(user, team, teamEffect).GetAwaiter().GetResult();
            return user;
        }

        public Task StartAsync(RoomItemStore.PlacedItem clock) => Games.UseClockAsync(clock, GameClockInteraction.StartStop);

        /// <summary>Steps the user onto (x, y) and runs the room's tick, as the grain does; <paramref name="arriving"/>
        /// when the walk ends there.</summary>
        public async Task StepAsync(RoomEntityStore.RoomEntity user, int x, int y, bool arriving = false)
        {
            var (fromX, fromY) = (user.X, user.Y);
            Entities.Warp(user, x, y);
            Games.UserSteppedOff(user, fromX, fromY);
            foreach (var item in Items.FloorItemsOn(x, y).ToList())
                await Games.UserSteppedOnAsync(user, item, fromX, fromY, arriving);
            await Games.PulseAsync();
        }

        /// <summary>Runs the work scheduled up to <paramref name="time"/> from now, in order.</summary>
        public async Task AdvanceAsync(TimeSpan time)
        {
            var until = _now + time;
            while (_scheduled.Where(s => s.At <= until).OrderBy(s => s.At).ThenBy(s => s.Order).FirstOrDefault() is { Action: not null } next)
            {
                _scheduled.Remove(next);
                _now = next.At;
                await next.Action();
            }

            _now = until;
        }

        public int State(int x, int y, string interaction) =>
            int.Parse(Items.FloorItemsOn(x, y).First(item => item.Item.Definition!.InteractionName == interaction).Data.StateText);

        public int Score(int team) => Games.Score(team);

        public List<(int X, int Y, string State)> Moves { get; } = [];

        private Task<bool> MoveAsync(RoomItemStore.PlacedItem item, int x, int y, int rotation, bool intoFurni)
        {
            if (!Items.CanPlaceFloorItem(item.Item.Id, item.Item.Definition!, x, y, rotation, (_, _) => false, intoFurni, false, anyDirection: true))
                return Task.FromResult(false);

            Items.MoveTo(item, x, y, rotation, intoFurni ? item.Z : null);
            Moves.Add((x, y, item.Data.StateText));
            return Task.FromResult(true);
        }

        public Task UpdateAsync(RoomItemStore.PlacedItem item, FurniDataView data)
        {
            item.Data = data;
            return Task.CompletedTask;
        }

        public Task SaveAsync(RoomItemStore.PlacedItem item, FurniDataView data)
        {
            item.Data = data;
            return Task.CompletedTask;
        }

        public void Schedule(TimeSpan delay, Func<Task> action) => _scheduled.Add((_now + delay, _order++, action));
        public void ShowData(RoomItemStore.PlacedItem item) { }
        public Task BatchedAsync(Func<Task> work) => work();
        public Task<bool> RemoveAsync(RoomItemStore.PlacedItem item) => Task.FromResult(Items.Remove(item.Item.Id));
        public Task GiveCreditsAsync(RoomEntityStore.RoomEntity user, int credits) => Task.CompletedTask;
        public void Broadcast(IComposer composer) => Broadcasts.Add(composer);
        public bool IsPlaced(RoomItemStore.PlacedItem item) => Items.Find(item.Item.Id) == item;
        public bool IsOccupied(RoomItemStore.PlacedItem item) => Entities.HasAvatarAt(item.X, item.Y);
        public void Tell(RoomEntityStore.RoomEntity user, IComposer composer) { }
        public bool IsWithinReach(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item) => true;
        public Task WalkThenAsync(RoomEntityStore.RoomEntity user, int x, int y, Func<Task> then) => then();
        public void LookAt(RoomEntityStore.RoomEntity user, int x, int y) { }
        public void SetHandItem(RoomEntityStore.RoomEntity user, int handItem) { }
        public void WearEffect(RoomEntityStore.RoomEntity user, int effectId) => user.EffectId = effectId;
        public void DressInRoom(RoomEntityStore.RoomEntity user, string? outfitFigure) { }
    }

    private sealed class NoEffects : IAvatarEffectCatalog
    {
        public Task<int?> FindAsync(string library, CancellationToken ct = default) => Task.FromResult<int?>(null);
        public Task<bool> ExistsAsync(int effectId, CancellationToken ct = default) => Task.FromResult(false);
    }

    // ---------- Battle Banzai ----------

    [Fact]
    public async Task AGameLightsEveryBanzaiTile()
    {
        var room = new Room();
        room.BanzaiArena(1, 1, 3, 3);
        await room.StartAsync(room.Clock(6, 6));

        Assert.All(room.Items.Items.Where(BanzaiArena.IsTile), tile => Assert.Equal("1", tile.Data.StateText));
    }

    [Fact]
    public async Task ThreeStepsOfATeamLockATileForAPoint()
    {
        var room = new Room();
        room.BanzaiArena(1, 1, 3, 3);
        var user = room.User(0, 1, Red);
        await room.StartAsync(room.Clock(6, 6));

        await room.StepAsync(user, 1, 1);
        Assert.Equal(3, room.State(1, 1, BanzaiTileInteraction.Name));
        await room.StepAsync(user, 2, 1);
        await room.StepAsync(user, 1, 1);
        Assert.Equal(4, room.State(1, 1, BanzaiTileInteraction.Name));
        Assert.Equal(0, room.Score(Red));
        await room.StepAsync(user, 2, 1);
        await room.StepAsync(user, 1, 1);

        Assert.Equal(5, room.State(1, 1, BanzaiTileInteraction.Name));
        Assert.Equal(1, room.Score(Red));
        await room.StepAsync(user, 2, 1);
        await room.StepAsync(user, 1, 1);
        Assert.Equal(5, room.State(1, 1, BanzaiTileInteraction.Name));
    }

    [Fact]
    public async Task StepsOutsideAGameOrTeamColourNothing()
    {
        var room = new Room();
        room.BanzaiArena(1, 1, 2, 1);
        var clock = room.Clock(6, 6);
        var player = room.User(0, 1, Red);
        var watcher = room.User(0, 2);

        await room.StepAsync(player, 1, 1);
        Assert.Equal(0, room.State(1, 1, BanzaiTileInteraction.Name));
        await room.StartAsync(clock);
        await room.StepAsync(watcher, 2, 1);
        Assert.Equal(1, room.State(2, 1, BanzaiTileInteraction.Name));
    }

    [Fact]
    public async Task AnotherTeamsUnlockedTileBecomesTheSteppersFirstLevel()
    {
        var room = new Room();
        room.BanzaiArena(1, 1, 2, 1);
        var red = room.User(0, 1, Red);
        var blue = room.User(0, 2, Blue, teamEffect: 35);
        await room.StartAsync(room.Clock(6, 6));

        await room.StepAsync(red, 1, 1);
        await room.StepAsync(red, 2, 1);
        await room.StepAsync(red, 1, 1);
        Assert.Equal(Red * 3 + 1, room.State(1, 1, BanzaiTileInteraction.Name));

        await room.StepAsync(blue, 1, 1);
        Assert.Equal(Blue * 3, room.State(1, 1, BanzaiTileInteraction.Name));
    }

    /// <summary>Habbo: (8,2) locked with its four direct neighbours, the ring's corners still at level 4.</summary>
    [Fact]
    public async Task ATileClosedOffByLockedTilesIsLockedWithThemForAPointEach()
    {
        var room = new Room();
        room.BanzaiArena(0, 0, 5, 5);
        var user = room.User(2, 1, Red);
        await room.StartAsync(room.Clock(7, 7));

        foreach (var (x, y) in new[] { (2, 1), (1, 2), (3, 2) })
            await LockAsync(room, user, x, y);
        Assert.Equal(1, room.State(2, 2, BanzaiTileInteraction.Name));
        Assert.Equal(3, room.Score(Red));

        await LockAsync(room, user, 2, 3);

        Assert.Equal(5, room.State(2, 2, BanzaiTileInteraction.Name));
        Assert.Equal(5, room.Score(Red));
    }

    [Fact]
    public async Task AnAreaReachingTheArenasEdgeIsNotClosedOff()
    {
        var room = new Room();
        room.BanzaiArena(0, 0, 3, 3);
        var user = room.User(1, 0, Red);
        await room.StartAsync(room.Clock(7, 7));

        // (1,1) has three locked neighbours; the fourth, (1,2), is open and at the edge
        foreach (var (x, y) in new[] { (1, 0), (0, 1), (2, 1) })
            await LockAsync(room, user, x, y);

        Assert.Equal(1, room.State(1, 1, BanzaiTileInteraction.Name));
        Assert.Equal(3, room.Score(Red));
    }

    [Fact]
    public async Task TilesLockedCountOnlyInAGameOfTwoTeams()
    {
        var solo = new Room();
        solo.BanzaiArena(1, 1, 2, 1);
        var alone = solo.User(0, 1, Red);
        await solo.StartAsync(solo.Clock(6, 6));
        await LockAsync(solo, alone, 1, 1);
        Assert.DoesNotContain(solo.Achievements, a => a.Achievement == AchievementNames.BattleBallTilesLocked);

        var duel = new Room();
        duel.BanzaiArena(1, 1, 2, 1);
        var red = duel.User(0, 1, Red);
        duel.User(0, 3, Blue, teamEffect: 35);
        await duel.StartAsync(duel.Clock(6, 6));
        await LockAsync(duel, red, 1, 1);
        Assert.Contains(duel.Achievements, a => a.User == red && a.Achievement == AchievementNames.BattleBallTilesLocked && a.Amount == 1);
    }

    [Fact]
    public async Task AtTheEndUntouchedTilesGoDarkAndTheWinnersFlashEndingLocked()
    {
        var room = new Room();
        room.BanzaiArena(1, 1, 3, 1);
        var user = room.User(0, 1, Red);
        var clock = room.Clock(6, 6);
        await room.StartAsync(clock);
        await LockAsync(room, user, 1, 1);
        await room.StepAsync(user, 2, 1);

        await room.Games.StopAsync(room.Games.Clock(clock)!);

        Assert.Equal(0, room.State(3, 1, BanzaiTileInteraction.Name));
        Assert.Equal(3, room.State(2, 1, BanzaiTileInteraction.Name));
        await room.AdvanceAsync(TimeSpan.FromMilliseconds(500));
        Assert.Equal(0, room.State(1, 1, BanzaiTileInteraction.Name));
        await room.AdvanceAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(5, room.State(1, 1, BanzaiTileInteraction.Name));
    }

    [Fact]
    public async Task ACounterThatRanOutStartsANewGameFromItsTime()
    {
        var room = new Room();
        room.BanzaiArena(1, 1, 2, 1);
        var item = room.Clock(6, 6);
        await room.StartAsync(item);
        var clock = room.Games.Clock(item)!;
        await room.Games.SetTimeAsync(clock, 1);
        await room.Games.PulseAsync();
        Assert.False(room.Games.Running);

        await room.StartAsync(item);

        Assert.True(room.Games.Running);
        Assert.Equal("30", item.Data.StateText);
        Assert.Equal(2, room.Games.GameNumber);
    }

    [Fact]
    public async Task WalkingIntoThePuckKicksItAheadColouringTheTilesItLandsOn()
    {
        var room = new Room(12, 4);
        room.BanzaiArena(0, 1, 12, 1);
        var puck = room.Furni(2, 1, "bb_puck", BanzaiPuckInteraction.Name, "6,1,0.5,0.0,0.8");
        var user = room.User(1, 1, Red);
        await room.StartAsync(room.Clock(0, 3));

        await room.StepAsync(user, 2, 1);
        await room.AdvanceAsync(TimeSpan.FromSeconds(5));
        await room.Games.PulseAsync();

        Assert.Equal((8, 1), (puck.X, puck.Y));
        Assert.Equal("11", puck.Data.StateText);
        Assert.Equal(Red * 3, room.State(5, 1, BanzaiTileInteraction.Name));
        Assert.Equal(1, room.State(9, 1, BanzaiTileInteraction.Name));
    }

    [Fact]
    public async Task ThePuckStopsAtAnAvatar()
    {
        var room = new Room(12, 4);
        var puck = room.Furni(2, 1, "bb_puck", BanzaiPuckInteraction.Name, "6");
        var user = room.User(1, 1);
        room.User(5, 1);

        await room.StepAsync(user, 2, 1);
        await room.AdvanceAsync(TimeSpan.FromSeconds(5));

        Assert.Equal((4, 1), (puck.X, puck.Y));
        Assert.Equal("11", puck.Data.StateText);
    }

    private static async Task LockAsync(Room room, RoomEntityStore.RoomEntity user, int x, int y)
    {
        var (fromX, fromY) = (user.X, user.Y);
        for (var step = 0; step < 3; step++)
        {
            await room.StepAsync(user, x, y);
            room.Entities.Warp(user, fromX, fromY);
        }
    }

    // ---------- Freeze ----------

    [Fact]
    public async Task AFreezeGameStartsPlayersOnThreeLivesAndTheirTeamOnFortyPoints()
    {
        var room = new Room();
        room.FreezeArena(1, 1, 3, 3);
        var block = room.Furni(2, 2, "es_box", FreezeBlockInteraction.Name, state: "11000");
        var user = room.User(1, 1, Red);
        await room.StartAsync(room.FreezeClock(6, 6));

        Assert.Equal(40, room.Score(Red));
        Assert.Contains(room.Broadcasts, b => b is GamePlayerValueMessageComposer { Value: 3 } v && v.RoomIndex == user.RoomIndex);
        Assert.Equal("0", block.Data.StateText);
    }

    [Fact]
    public async Task ABanzaiCounterBesideAFreezeArenaStartsNoGameOfFreeze()
    {
        var room = new Room();
        room.FreezeArena(1, 1, 3, 3);
        var user = room.User(2, 2, Red);
        await room.StartAsync(room.Clock(6, 6));

        Assert.Equal(0, room.Score(Red));
        await room.Games.ThrowAsync(user);
        Assert.Equal(0, room.State(2, 2, FreezeTileInteraction.Name));
    }

    [Fact]
    public async Task ASnowballBurstsTwoSecondsLaterInACrossOneTileFurtherEachStep()
    {
        var room = new Room();
        room.FreezeArena(1, 1, 3, 3);
        var user = room.User(1, 1, Red);
        await room.StartAsync(room.FreezeClock(6, 6));
        await room.StepAsync(user, 2, 2);

        await room.Games.ThrowAsync(user);
        Assert.Equal(1000, room.State(2, 2, FreezeTileInteraction.Name));
        room.Entities.Warp(user, 1, 1);
        await room.AdvanceAsync(TimeSpan.FromMilliseconds(1999));
        Assert.Equal(1000, room.State(2, 2, FreezeTileInteraction.Name));
        await room.AdvanceAsync(TimeSpan.FromMilliseconds(1));

        Assert.Equal(11100, room.State(2, 2, FreezeTileInteraction.Name));
        Assert.Equal(11100, room.State(3, 2, FreezeTileInteraction.Name));
        Assert.Equal(11100, room.State(2, 1, FreezeTileInteraction.Name));
        Assert.Equal(0, room.State(1, 1, FreezeTileInteraction.Name));
    }

    [Fact]
    public async Task AHitCostsALifeAndTenPointsAndFreezesThenProtects()
    {
        var room = new Room();
        room.FreezeArena(1, 1, 3, 3);
        var user = room.User(2, 2, Red);
        await room.StartAsync(room.FreezeClock(6, 6));

        await room.Games.ThrowAsync(user);
        await room.AdvanceAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(30, room.Score(Red));
        Assert.Contains(room.Broadcasts, b => b is GamePlayerValueMessageComposer { Value: 2 });
        Assert.True(user.Frozen);
        Assert.Equal(12, user.EffectId);
        await room.AdvanceAsync(TimeSpan.FromSeconds(5));
        Assert.False(user.Frozen);
        Assert.Equal(49, user.EffectId);
        await room.AdvanceAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(40, user.EffectId);
    }

    [Fact]
    public async Task AHitOnAnotherTeamWinsTheThrowerThePoints()
    {
        var room = new Room();
        room.FreezeArena(1, 1, 3, 3);
        var red = room.User(2, 2, Red);
        var blue = room.User(3, 2, Blue, teamEffect: 43);
        await room.StartAsync(room.FreezeClock(6, 6));

        await room.Games.ThrowAsync(red);
        room.Entities.Warp(red, 1, 1);
        await room.AdvanceAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(50, room.Score(Red));
        Assert.Equal(30, room.Score(Blue));
        Assert.True(blue.Frozen);
    }

    [Fact]
    public async Task ABurstBreaksAWholeBlockIntoAPowerUpAndStopsThere()
    {
        var room = new Room();
        room.FreezeArena(1, 1, 4, 1);
        var block = room.Furni(2, 1, "es_box", FreezeBlockInteraction.Name, "0.0,1.0");
        var user = room.User(1, 1, Red);
        await room.StartAsync(room.FreezeClock(6, 6));
        // a range of 2 would reach (3,1) but for the block
        await PickUpAsync(room, user, RangeBlock(room, 0, 3), 0, 3);
        room.Entities.Warp(user, 1, 1);

        await room.Games.ThrowAsync(user);
        room.Entities.Warp(user, 0, 0);
        await room.AdvanceAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(2100, int.Parse(block.Data.StateText));
        Assert.Equal(0, room.State(3, 1, FreezeTileInteraction.Name));
    }

    [Fact]
    public async Task SteppingOnAPowerUpPicksItUp()
    {
        var room = new Room();
        room.FreezeArena(1, 1, 4, 1);
        var user = room.User(1, 1, Red);
        await room.StartAsync(room.FreezeClock(6, 6));
        var block = RangeBlock(room, 2, 1);

        await room.StepAsync(user, 2, 1);

        Assert.Equal("12000", block.Data.StateText);
        Assert.Contains(room.Achievements, a => a.User == user && a.Achievement == AchievementNames.FreezePowerUp);
        // the range power-up: the next snowball is the big one, and reaches two tiles
        await room.Games.ThrowAsync(user);
        Assert.Equal(2000, room.State(2, 1, FreezeTileInteraction.Name));
        room.Entities.Warp(user, 0, 0);
        await room.AdvanceAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(11200, room.State(4, 1, FreezeTileInteraction.Name));
    }

    [Fact]
    public async Task AWholeBlockKeepsAvatarsOff()
    {
        var room = new Room();
        var block = room.Furni(2, 1, "es_box", FreezeBlockInteraction.Name);

        Assert.True(room.Items.BlocksWalking(2, 1));
        block.Data = FurniDataView.WithState("2000");
        Assert.False(room.Items.BlocksWalking(2, 1));
    }

    [Fact]
    public async Task APlayerOutOfLivesGoesToTheExitAndTheLastTeamLeftEndsTheGame()
    {
        var room = new Room();
        room.FreezeArena(1, 1, 3, 3);
        var exit = room.Furni(0, 5, "es_exit", FreezeExitInteraction.Name);
        var red = room.User(2, 2, Red);
        var blue = room.User(1, 1, Blue, teamEffect: 43);
        var clock = room.FreezeClock(6, 6);
        await room.StartAsync(clock);
        Assert.Equal("1", exit.Data.StateText);

        for (var life = 0; life < 3; life++)
        {
            await room.Games.ThrowAsync(red);
            await room.AdvanceAsync(TimeSpan.FromSeconds(10));
            if (red.Team != RoomGames.NoTeam)
                room.Entities.Warp(red, 2, 2);
        }

        Assert.Equal(RoomGames.NoTeam, red.Team);
        Assert.Equal((0, 5), (red.X, red.Y));
        Assert.False(room.Games.Running);
        Assert.Equal("0", exit.Data.StateText);
        Assert.Contains(room.Achievements, a => a.User == blue && a.Achievement == AchievementNames.FreezeWinner);
        // the counter ran out, so a double-click starts the next game afresh
        Assert.Equal("0", clock.Data.StateText);
        await room.StartAsync(clock);
        Assert.True(room.Games.Running);
    }

    /// <summary>A block already broken into the range power-up, on (x, y).</summary>
    private static RoomItemStore.PlacedItem RangeBlock(Room room, int x, int y) =>
        room.Furni(x, y, "es_box", FreezeBlockInteraction.Name, state: "2000");

    private static async Task PickUpAsync(Room room, RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem block, int x, int y) =>
        await room.StepAsync(user, x, y);

    // ---------- football ----------

    /// <summary>A football, with artwork for directions 0 to 3 only, as Habbo's has.</summary>
    private static RoomItemStore.PlacedItem Ball(Room room, int x, int y)
    {
        var ball = room.Furni(x, y, "fball_ball", FootballInteraction.Name, "6,1,0.5,0.5");
        ball.Item.Definition!.Directions = [.. Enumerable.Range(0, 4).Select(d => new ItemDefinitionDirection { Direction = d })];
        return ball;
    }

    /// <summary>Habbo: a walk ending on the ball sent it 6 tiles, 55, 44, 33, 22, 11, 11.</summary>
    [Fact]
    public async Task AWalkEndingOnTheBallKicksItSixTilesSlowingDown()
    {
        var room = new Room(12, 20);
        var ball = Ball(room, 5, 10);
        var user = room.User(5, 11);

        await room.StepAsync(user, 5, 10, arriving: true);
        await room.AdvanceAsync(TimeSpan.FromSeconds(5));

        Assert.Equal((5, 4), (ball.X, ball.Y));
        Assert.Equal(["55", "44", "33", "22", "11", "11"], room.Moves.Select(m => m.State));
        Assert.Equal(0, ball.Rotation);
    }

    /// <summary>Habbo: from (8,7) to (8,5) over the ball on (8,6), the ball went to (8,5) and the player stopped on it;
    /// walking off north then heeled it six tiles south.</summary>
    [Fact]
    public async Task AWalkOverTheBallDribblesItAndOneEndingOnTheDribbledBallStandsOnIt()
    {
        var room = new Room(12, 20);
        var ball = Ball(room, 5, 6);
        var user = room.User(5, 7);

        await room.StepAsync(user, 5, 6);
        Assert.Equal((5, 5), (ball.X, ball.Y));
        Assert.Equal("11", ball.Data.StateText);
        await room.StepAsync(user, 5, 5, arriving: true);
        await room.AdvanceAsync(TimeSpan.FromSeconds(5));
        Assert.Equal((5, 5), (ball.X, ball.Y));

        await room.StepAsync(user, 5, 4, arriving: true);
        await room.AdvanceAsync(TimeSpan.FromSeconds(5));

        Assert.Equal((5, 11), (ball.X, ball.Y));
        Assert.Equal(4, ball.Rotation);
    }

    /// <summary>Habbo: into the goal at 44, the goal lit and its team's scoreboard counted, the kicker waved, and the
    /// ball came back out a tile at 22.</summary>
    [Fact]
    public async Task ABallRolledIntoAGoalScoresForItsTeamAndComesBackOut()
    {
        var room = new Room(12, 20);
        var goal = room.Furni(4, 2, "fball_goal_b", FootballGoalInteraction.Name, "3", canWalk: false, width: 3);
        // open to the south, as Habbo's blue goal at dir 4 that took a ball rolling north
        goal.Rotation = 4;
        var blue = room.Furni(0, 0, "fball_score_b", HockeyScoreInteraction.Name, "3,99", canWalk: false);
        var red = room.Furni(0, 1, "fball_score_r", HockeyScoreInteraction.Name, "1,99", canWalk: false);
        var ball = Ball(room, 5, 5);
        var user = room.User(5, 6);

        await room.StepAsync(user, 5, 5, arriving: true);
        // in the net on the third tile (0.08 + 0.10 + 0.125 s), out again 0.17 s later
        await room.AdvanceAsync(TimeSpan.FromMilliseconds(350));

        Assert.Equal((5, 2), (ball.X, ball.Y));
        Assert.Equal("1", goal.Data.StateText);
        Assert.Equal("1", blue.Data.StateText);
        Assert.Equal("0", red.Data.StateText);
        Assert.Contains(room.Broadcasts, b => b is Habbo.Protocol.Messages.Outgoing.Room.Action.ExpressionMessageComposer { Expression: 1 } e && e.RoomIndex == user.RoomIndex);
        Assert.Contains(room.Achievements, a => a.User == user && a.Achievement == AchievementNames.FootballGoalScored);
        Assert.Contains(AchievementNames.FootballGoalScoredInRoom, room.OwnerAchievements);

        await room.AdvanceAsync(TimeSpan.FromSeconds(5));
        Assert.Equal((5, 3), (ball.X, ball.Y));
        Assert.Equal("11", ball.Data.StateText);
        Assert.Equal("0", goal.Data.StateText);
    }

    /// <summary>Habbo: a goal at dir 0 on (7,7) hit from the south, the ball at (8,8) at 44 went back to (8,9) at 22, and
    /// nothing scored.</summary>
    [Fact]
    public async Task AGoalHitFromBehindSendsTheBallBackAndScoresNothing()
    {
        var room = new Room(14, 14);
        var goal = room.Furni(7, 7, "fball_goal_b", FootballGoalInteraction.Name, "3", canWalk: false, width: 3);
        var board = room.Furni(0, 0, "fball_score_b", HockeyScoreInteraction.Name, "3,99", canWalk: false);
        var ball = Ball(room, 8, 10);
        var user = room.User(8, 11);

        await room.StepAsync(user, 8, 10, arriving: true);
        room.Entities.Warp(user, 0, 13);
        await room.AdvanceAsync(TimeSpan.FromSeconds(3));

        Assert.Equal([(8, 9, "55"), (8, 8, "44"), (8, 9, "22")], room.Moves);
        Assert.Equal("0", goal.Data.StateText);
        Assert.Equal("0", board.Data.StateText);
        Assert.Equal(4, ball.Rotation);
    }

    /// <summary>Habbo: at the wall at 33, the ball turned back and rolled two more tiles at 11.</summary>
    [Fact]
    public async Task ABallTurnsBackOffAWallATileShorter()
    {
        var room = new Room(8, 8);
        var ball = Ball(room, 5, 3);
        var user = room.User(4, 3);

        await room.StepAsync(user, 5, 3, arriving: true);
        room.Entities.Warp(user, 0, 7);
        await room.AdvanceAsync(TimeSpan.FromSeconds(5));

        Assert.Equal([(6, 3, "55"), (7, 3, "44"), (6, 3, "11"), (5, 3, "11"), (4, 3, "11")], room.Moves);
        Assert.Equal(6, ball.Rotation);
    }

    [Fact]
    public async Task AScoreboardCountsUpAndDownAndClears()
    {
        var room = new Room();
        var board = room.Furni(0, 0, "fball_score_b", HockeyScoreInteraction.Name, "3,2", canWalk: false);
        var user = room.User(1, 1);
        Task Use(int param) => board.Interaction.UseAsync(new FurniUse(board, user, default, param, room));

        await Use(2);
        await Use(2);
        await Use(2);
        Assert.Equal("2", board.Data.StateText);
        await Use(1);
        Assert.Equal("1", board.Data.StateText);
        await Use(3);
        Assert.Equal("0", board.Data.StateText);
    }
}
