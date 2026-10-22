using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Abstractions.Wired;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired;
using Habbo.Silo.Wired.Variables;
using Habbo.Silo.Wired.Addons;
using Habbo.Silo.Wired.Conditions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Habbo.Silo.Tests;

public class RoomWiredTests
{
    private static readonly FurniInteractionRegistry Interactions = new();
    private static readonly WiredBoxRegistry Boxes = new();

    private const string Trigger = "wired_trigger";
    private const string Effect = "wired_effect";
    private const string Condition = "wired_condition";
    private const string Selector = "wired_selector";
    private const string Addon = "wired_addon";

    /// <summary>A room of wired: items, users, and what the room was asked to do.</summary>
    private sealed class Room : IFurniRoom
    {
        private int _nextId = 1;

        public RoomItemStore Items { get; }
        public RoomEntityStore Entities { get; }
        public RoomWired Wired { get; }
        public RoomGames Games { get; }
        public List<IComposer> Broadcasts { get; } = [];
        public List<(Guid To, IComposer Message)> Sent { get; } = [];
        public int StateUpdates { get; private set; }
        public List<RoomEntityStore.RoomEntity> Kicked { get; } = [];
        public List<(byte Level, byte Source, string Message)> Logs { get; } = [];
        public List<(RoomEntityStore.RoomEntity User, RoomItemStore.PlacedItem Teleport)> SentThrough { get; } = [];
        public List<(RoomEntityStore.RoomEntity Bot, string Text, Habbo.Silo.Abstractions.Chat.RoomChatKind Kind, int Width,
            IReadOnlyList<RoomEntityStore.RoomEntity>? Listeners)> Said { get; } = [];
        public Dictionary<string, string> Texts { get; } = [];
        public NoChests Chests { get; } = new();

        public Room()
        {
            var model = RoomModelParser.Parse(
                "00000000" + "\r" +
                "00000000" + "\r" +
                "00000000" + "\r" +
                "00000000",
                doorX: 0, doorY: 0);
            Items = new RoomItemStore(model, Interactions);
            Entities = new RoomEntityStore(model, Items);
            Games = new RoomGames(Items, Entities, this, new Effects(), (user, effect) => user.EffectId = effect);
            Wired = new RoomWired(Guid.Empty, Items, Entities, Games, Boxes, this, dbFactory: null!,
                Broadcasts.Add, (to, message) => Sent.Add((to, message)),
                new WiredRoomActions(MoveAsync, SetAltitudeAsync, (user, effect) => user.EffectId = effect, KickAsync, new GameTexts(Texts),
                    (level, source, message) =>
                    {
                        Logs.Add((level, source, message));
                        return Task.CompletedTask;
                    },
                    (user, style) =>
                    {
                        Entities.Dance(user, style);
                        return Task.CompletedTask;
                    },
                    (user, sign) =>
                    {
                        Entities.ShowSign(user, sign);
                        return Task.CompletedTask;
                    },
                    _ => Task.CompletedTask,
                    new Effects(),
                    Chests,
                    new NoRewards())
                {
                    Bots = new WiredBotActions(
                        (bot, text, kind, width, listeners) =>
                        {
                            Said.Add((bot, text, kind, width, listeners));
                            return Task.CompletedTask;
                        },
                        (bot, figure) =>
                        {
                            bot.Bot!.Data.Figure = figure;
                            return Task.CompletedTask;
                        },
                        (giver, receiver, handItem) =>
                        {
                            receiver.HandItem = handItem;
                            return Task.CompletedTask;
                        },
                        // the walk up is done at once: what a walk looks like is the room's, not wired's
                        (walker, target, then) =>
                        {
                            Entities.Warp(walker, target.X - 1, target.Y);
                            return then();
                        }),
                    SendThrough = (user, teleport) =>
                    {
                        SentThrough.Add((user, teleport));
                        return Task.CompletedTask;
                    },
                },
                NullLogger.Instance);
            Games.OnEvent = Wired.GameEventAsync;
        }

        public RoomItemStore.PlacedItem Furni(int x, int y, string className = "lamp", string logic = "furniture_multistate")
        {
            var definition = new ItemDefinition
            {
                Id = className.GetHashCode(),
                ClassName = className,
                Logic = logic,
                CanWalk = true,
                CanStackOn = true,
                StackHeight = 0.5,
                States = [new ItemDefinitionState { State = 0 }, new ItemDefinitionState { State = 1 }],
            };
            var id = _nextId++;
            return Items.Add(new UserItem { Id = id, Definition = definition, DefinitionId = definition.Id },
                FurniDataView.WithState("0"), x, y, rotation: 0);
        }

        /// <summary>A wired box on (x, y), on top of whatever is there already.</summary>
        public RoomItemStore.PlacedItem Box(int x, int y, string className, string family, FurniWired settings)
        {
            var definition = new ItemDefinition
            {
                Id = className.GetHashCode(),
                ClassName = className,
                Logic = "furniture_multistate",
                Interaction = family,
                CanStackOn = true,
                StackHeight = 0.65,
                States = [new ItemDefinitionState { State = 0 }, new ItemDefinitionState { State = 1 }],
            };
            var id = _nextId++;
            var placed = Items.Add(new UserItem { Id = id, Definition = definition, DefinitionId = definition.Id },
                FurniDataView.WithState("0"), x, y, rotation: 0);
            settings.ItemId = id;
            Wired.Add(placed, settings);
            return placed;
        }

        public RoomEntityStore.RoomEntity User(int x, int y, string name = "tester")
        {
            var player = Player(name);
            var user = Entities.Enter(player.Id, Guid.NewGuid(), player, doorDirection: 2);
            Entities.Warp(user, x, y);
            return user;
        }

        /// <summary>A bot of the name on (x, y).</summary>
        public RoomEntityStore.RoomEntity Bot(int x, int y, string name)
        {
            var data = new Habbo.Domain.Bots.Bot { Id = _nextId++, Name = name, Figure = "hd-180-1", Type = new Habbo.Domain.Bots.BotType() };
            return Entities.AddBot(new RoomBot { Data = data, OwnerAccountId = 0, OwnerName = string.Empty }, x, y, 0, 2);
        }

        /// <summary>Steps the user onto (x, y) and tells the wired, as a tick would.</summary>
        public Task StepAsync(RoomEntityStore.RoomEntity user, int x, int y)
        {
            var (fromX, fromY) = (user.X, user.Y);
            Entities.Warp(user, x, y);
            return Wired.UserSteppedAsync(user, fromX, fromY);
        }

        public List<WiredMoveOptions> MoveOptions { get; } = [];

        private Task<bool> MoveAsync(RoomItemStore.PlacedItem item, int x, int y, int rotation, WiredMoveOptions options)
        {
            MoveOptions.Add(options);
            Func<int, int, bool> avatarAt = options.AvatarsBlock ? Entities.HasAvatarAt : (_, _) => false;
            if (!Items.CanPlaceFloorItem(item.Item.Id, item.Item.Definition!, x, y, rotation, avatarAt, options.ThroughFurni, options.BlockedByFurni))
                return Task.FromResult(false);

            Items.MoveTo(item, x, y, rotation, options.KeepAltitude ? item.Z : null);
            return Task.FromResult(true);
        }

        private static Task<bool> SetAltitudeAsync(RoomItemStore.PlacedItem item, double altitude)
        {
            if (altitude < 0 || altitude > RoomItemStore.MaxAltitude)
                return Task.FromResult(false);

            item.Z = altitude;
            return Task.FromResult(true);
        }

        private Task KickAsync(RoomEntityStore.RoomEntity user)
        {
            Kicked.Add(user);
            Entities.Leave(user.Player.Id);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(RoomItemStore.PlacedItem item, FurniDataView data)
        {
            var changed = item.Data.State != data.State;
            item.Data = data;
            StateUpdates++;
            return changed ? Wired.FurniStateSetAsync(item) : Task.CompletedTask;
        }

        public Task SaveAsync(RoomItemStore.PlacedItem item, FurniDataView data)
        {
            item.Data = data;
            return Task.CompletedTask;
        }

        public Task<bool> RemoveAsync(RoomItemStore.PlacedItem item) => Task.FromResult(Items.Remove(item.Item.Id));
        public Task GiveCreditsAsync(RoomEntityStore.RoomEntity user, int credits) => Task.CompletedTask;
        public void Broadcast(IComposer composer) => Broadcasts.Add(composer);
        public void Schedule(TimeSpan delay, Func<Task> action) { }
        public void ShowData(RoomItemStore.PlacedItem item) => Broadcasts.Add(new Habbo.Protocol.Messages.Outgoing.Room.Engine.ObjectDataUpdateMessageComposer(item.Item.Id, Habbo.Protocol.Messages.Outgoing.Room.Engine.FurniStuffData.From(item.Data)));
        public Task BatchedAsync(Func<Task> work) => work();
        public bool IsOccupied(RoomItemStore.PlacedItem item) => Entities.HasAvatarAt(item.X, item.Y);
        public void Tell(RoomEntityStore.RoomEntity user, IComposer composer) => Sent.Add((user.Player.Id, composer));
        public bool IsPlaced(RoomItemStore.PlacedItem item) => true;
        public bool IsWithinReach(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item) => true;

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

    /// <summary>The team effects by the names Habbo's effect map gives them.</summary>
    /// <summary>A room without chests, and without rewards to give.</summary>
    private sealed class NoChests : IWiredChests
    {
        /// <summary>Who the coins effects asked to pay, though there is nothing to pay from.</summary>
        public List<RoomEntityStore.RoomEntity> Paid { get; } = [];

        public bool IsUsable(RoomItemStore.PlacedItem item) => false;
        public Habbo.Silo.Chests.ChestKind KindOf(RoomItemStore.PlacedItem item) => new(false, item.Item.Definition!.ClassId, null);
        public int Count(RoomItemStore.PlacedItem chest, IReadOnlySet<Habbo.Silo.Chests.ChestKind>? kinds = null, bool previewedOnly = false) => 0;

        public Task GiveCoinsAsync(IReadOnlyList<RoomItemStore.PlacedItem> chests, RoomEntityStore.RoomEntity user, int? amount, byte earningsCategory, WiredChestGift gift)
        {
            Paid.Add(user);
            return Task.CompletedTask;
        }

        public Task GiveItemsAsync(IReadOnlyList<RoomItemStore.PlacedItem> chests, RoomEntityStore.RoomEntity user, int? amount, int iteration, WiredChestGift gift) => Task.CompletedTask;
        public Task<Habbo.Domain.Furniture.Chests.WiredContract?> ContractOfAsync(RoomItemStore.PlacedItem item) => Task.FromResult<Habbo.Domain.Furniture.Chests.WiredContract?>(null);
        public Task StartContractAsync(RoomEntityStore.RoomEntity user, Habbo.Silo.Chests.ContractTerms terms, int timeoutSeconds) => Task.CompletedTask;
        public Task CancelContractsAsync(RoomEntityStore.RoomEntity user, IReadOnlySet<int>? contractIds) => Task.CompletedTask;
        public void Fail(RoomEntityStore.RoomEntity user, int failure) { }
    }

    private sealed class NoRewards : Habbo.Silo.Wired.Rewards.IWiredRewards
    {
        public Task RewardAsync(int boxId, RoomEntityStore.RoomEntity user, Habbo.Silo.Wired.Rewards.WiredRewardRules rules, Random random) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> MissingAsync(IReadOnlyList<Habbo.Silo.Wired.Rewards.WiredReward> rewards) => Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class Effects : IAvatarEffectCatalog
    {
        private static readonly Dictionary<string, int> ByLibrary = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Twinkle"] = 4, ["BlueTwinkle"] = 235,
            ["WiredTeamRed"] = 223, ["WiredTeamBlue"] = 224, ["WiredTeamYellow"] = 225, ["WiredTeamGreen"] = 226,
        };

        public Task<int?> FindAsync(string library, CancellationToken ct = default) =>
            Task.FromResult(ByLibrary.TryGetValue(library, out var id) ? id : (int?)null);

        public Task<bool> ExistsAsync(int effectId, CancellationToken ct = default) => Task.FromResult(ByLibrary.ContainsValue(effectId));
    }

    private sealed class GameTexts(Dictionary<string, string> texts) : IGameTexts
    {
        public Task<string?> GetAsync(string key, CancellationToken ct = default) => Task.FromResult(texts.GetValueOrDefault(key));
    }

    private static PlayerData Player(string name) => new(
        Guid.NewGuid(), 1, name, "hd-180-1", Gender.Male, "", 0, SecurityLevel.AboveNone, "", false, 0, 3, 3, true, false, false,
        ClubLevel.None, false, 0, false, DateTimeOffset.UtcNow, PlayerData.NoHomeRoom, 0, false, "");

    private static FurniWired Picks(params RoomItemStore.PlacedItem[] items) => new() { StuffIds = items.Select(i => i.Item.Id).ToList() };

    [Fact]
    public async Task WalkingOnAPickedFurniRunsTheEffectsStackedWithTheTrigger()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var lamp = room.Furni(6, 1);
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(1, 1, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task AnEffectOnAnotherTileIsNotPartOfTheStack()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var lamp = room.Furni(6, 1);
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(1, 2, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal("0", lamp.Data.State);
    }

    [Fact]
    public async Task AFailingConditionStopsTheStack()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var mat = room.Furni(5, 3, "mat");
        var lamp = room.Furni(6, 1);
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(1, 1, "wf_cnd_trggrer_on_frn", Condition, Picks(mat));
        room.Box(1, 1, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal("0", lamp.Data.State);
    }

    [Fact]
    public async Task EveryConditionMustPass()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var lamp = room.Furni(6, 1);
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(1, 1, "wf_cnd_trggrer_on_frn", Condition, Picks(plate));
        room.Box(1, 1, "wf_cnd_user_count_in", Condition, new FurniWired { IntParams = [1, 1] });
        room.Box(1, 1, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);
        Assert.Equal("1", lamp.Data.State);

        room.User(0, 3, "second");
        await room.StepAsync(user, 2, 1);
        await room.StepAsync(user, 3, 1);
        Assert.Equal("1", lamp.Data.State); // two users now: the count condition fails
    }

    [Fact]
    public async Task ADelayedEffectRunsAfterItsPulses()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var lamp = room.Furni(6, 1);
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        var toggle = Picks(lamp);
        toggle.DelayInPulses = 2;
        room.Box(1, 1, "wf_act_toggle_state", Effect, toggle);
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);
        Assert.Equal("0", lamp.Data.State);
        await room.Wired.PulseAsync();
        Assert.Equal("0", lamp.Data.State);
        await room.Wired.PulseAsync();
        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task ARepeatTriggerFiresEveryPeriod()
    {
        var room = new Room();
        var lamp = room.Furni(6, 1);
        room.Box(1, 1, "wf_trg_periodically", Trigger, new FurniWired { IntParams = [2] });
        room.Box(1, 1, "wf_act_toggle_state", Effect, Picks(lamp));

        for (var pulse = 0; pulse < 5; pulse++)
            await room.Wired.PulseAsync();

        // pulses 1, 3 and 5
        Assert.Equal(3, room.StateUpdates);
    }

    [Fact]
    public async Task ASelectorChoosesTheUsersAnEffectWorksOn()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var pad = room.Furni(4, 3, "pad");
        var target = room.Furni(7, 3, "target");
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(1, 1, "wf_slc_users_onfurni", Selector, Picks(pad));
        var teleport = Picks(target);
        teleport.UserSources = [WiredSources.SelectedUsers];
        room.Box(1, 1, "wf_act_teleport_to", Effect, teleport);
        var walker = room.User(2, 1, "walker");
        var waiting = room.User(4, 3, "waiting");

        await room.StepAsync(walker, 3, 1);
        for (var pulse = 0; pulse < 4; pulse++)
            await room.Wired.PulseAsync();

        // the user the selector chose moves; the one who set the stack off does not
        Assert.Equal((7, 3), (waiting.X, waiting.Y));
        Assert.Equal((3, 1), (walker.X, walker.Y));
    }

    [Fact]
    public async Task AFilterSelectorKeepsOnlyWhatItAlsoChooses()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var pad = room.Furni(4, 3, "pad");
        var target = room.Furni(7, 3, "target");
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(1, 1, "wf_slc_users_onfurni", Selector, Picks(pad));
        room.Box(1, 1, "wf_slc_users_byname", Selector, new FurniWired { StringParam = "alice", IsFilter = true });
        var teleport = Picks(target);
        teleport.UserSources = [WiredSources.SelectedUsers];
        room.Box(1, 1, "wf_act_teleport_to", Effect, teleport);
        var alice = room.User(4, 3, "alice");
        var bob = room.User(4, 3, "bob");

        await room.StepAsync(room.User(2, 1, "walker"), 3, 1);
        for (var pulse = 0; pulse < 4; pulse++)
            await room.Wired.PulseAsync();

        Assert.Equal((7, 3), (alice.X, alice.Y));
        Assert.Equal((4, 3), (bob.X, bob.Y));
    }

    [Fact]
    public async Task TheRandomAddonRunsOneOfTheEffects()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var lamps = new[] { room.Furni(5, 0), room.Furni(6, 0), room.Furni(7, 0) };
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        foreach (var lamp in lamps)
            room.Box(1, 1, "wf_act_toggle_state", Effect, Picks(lamp));
        room.Box(1, 1, "wf_xtra_random", Addon, new FurniWired { IntParams = [0, 1] });
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal(1, lamps.Count(lamp => lamp.Data.State == "1"));
    }

    [Fact]
    public async Task StacksThatSetEachOtherOffAreStopped()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        var first = room.Box(1, 1, "wf_act_call_stacks", Effect, new FurniWired());
        var second = room.Box(1, 3, "wf_act_call_stacks", Effect, Picks(first));
        room.Wired.Add(first, new FurniWired { ItemId = first.Item.Id, StuffIds = [second.Item.Id] });
        var user = room.User(2, 1);

        // each stack runs the other; without a bound this would never return
        await room.StepAsync(user, 3, 1);
    }

    [Fact]
    public async Task APerformedActionRunsTheStackWaitingForIt()
    {
        var room = new Room();
        var lamp = room.Furni(6, 1);
        room.Box(1, 1, "wf_trg_user_performs_action", Trigger,
            new FurniWired { IntParams = [WiredUserActions.Sign], StringParam = WiredUserActions.SignExtra(4) });
        room.Box(1, 1, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        await room.Wired.UserActedAsync(user, WiredUserActions.Sign, WiredUserActions.SignExtra(3));
        Assert.Equal("0", lamp.Data.State);

        await room.Wired.UserActedAsync(user, WiredUserActions.Sign, WiredUserActions.SignExtra(4));
        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task APerformingActionConditionChecksWhatTheUserIsDoingNow()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var lamp = room.Furni(6, 1);
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(1, 1, "wf_cnd_user_performs_action", Condition, new FurniWired { IntParams = [WiredUserActions.Dance] });
        room.Box(1, 1, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);
        Assert.Equal("0", lamp.Data.State);

        room.Entities.Dance(user, 1);
        await room.StepAsync(user, 2, 1);
        await room.StepAsync(user, 3, 1);
        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task ASignalRunsTheStacksListeningOnItsAntennaWithWhatItCarries()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var antenna = room.Furni(0, 3, "antenna", logic: "furniture_multistate");
        antenna.Item.Definition!.Interaction = Habbo.Silo.Items.Interactions.WiredAntennaInteraction.Name;
        var target = room.Furni(7, 3, "target");

        // stack 1: walking on the plate sends the walker through the antenna
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        var send = Picks(antenna);
        send.UserSources = [WiredSources.Triggerer];
        room.Box(1, 1, "wf_act_send_signal", Effect, send);

        // stack 2, elsewhere: a received signal teleports the users it carried
        room.Box(5, 0, "wf_trg_recv_signal", Trigger, Picks(antenna));
        room.Box(5, 0, "wf_slc_users_signal", Selector, new FurniWired());
        var teleport = Picks(target);
        teleport.UserSources = [WiredSources.SelectedUsers];
        room.Box(5, 0, "wf_act_teleport_to", Effect, teleport);
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);
        for (var pulse = 0; pulse < 4; pulse++)
            await room.Wired.PulseAsync();

        Assert.Equal((7, 3), (user.X, user.Y));
    }

    private static RoomItemStore.PlacedItem Antenna(Room room, int x, int y)
    {
        var antenna = room.Furni(x, y, "antenna", logic: "furniture_multistate");
        antenna.Item.Definition!.Interaction = Habbo.Silo.Items.Interactions.WiredAntennaInteraction.Name;
        return antenna;
    }

    /// <summary>A change-variable box's settings that add one to a room variable.</summary>
    private static FurniWired AddOne(string variable) => new()
    {
        VariableIds = [variable, ""],
        IntParams = [WiredVariableTargets.Global, WiredVariableUse.Add, 0, 0, 1, WiredVariableTargets.Global],
    };

    /// <summary>A variable value condition's settings: the room's value of the variable compared with a number.</summary>
    private static FurniWired RoomValueIs(string variable, int comparison, int number) => new()
    {
        VariableIds = [variable, ""],
        IntParams = [WiredVariableTargets.Global, comparison, 0, 0, number, WiredVariableTargets.Global],
    };

    /// <summary>Stacks on the first <paramref name="count"/> tiles of rows 0 to 2, each adding one to "visits" when
    /// its antenna hears a signal and passing it on to the next one's, the last back to the first; walking onto
    /// the plate at (3, 3) sends the first signal. The library's p24_chain.</summary>
    private static (Room Room, RoomEntityStore.RoomEntity User, WiredVariable Visits) Ring(int count)
    {
        var room = new Room();
        var visits = Variable(room, "wf_var_room", new FurniWired { StringParam = "visits", IntParams = [1] });
        var antennas = Enumerable.Range(0, count).Select(i => Antenna(room, 4 + i % 3, 3)).ToList();
        for (var i = 0; i < count; i++)
        {
            var (x, y) = (i % 8, i / 8);
            room.Box(x, y, "wf_trg_recv_signal", Trigger, Picks(antennas[i]));
            room.Box(x, y, "wf_xtra_exec_in_order", Addon, new FurniWired());
            room.Box(x, y, "wf_act_change_var_val", Effect, AddOne(visits));
            room.Box(x, y, "wf_act_send_signal", Effect, Picks(antennas[(i + 1) % count]));
        }

        var plate = room.Furni(3, 3, "plate");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_act_send_signal", Effect, Picks(antennas[0]));
        return (room, room.User(2, 3), room.Wired.FindVariable(visits)!);
    }

    [Theory]
    [InlineData(2, 200)]
    [InlineData(12, 1200)]
    [InlineData(24, 2400)]
    public async Task OneExecutionRunsEachStackAHundredTimes(int stacks, int visits)
    {
        var (room, user, counter) = Ring(stacks);

        await room.StepAsync(user, 3, 3);

        Assert.Equal(visits, room.Wired.VariableValue(counter, WiredHolder.Room, null));
        Assert.Equal(WiredErrors.ExecutionLimit, Assert.Single(room.Wired.Errors).Kind);
    }

    [Fact]
    public async Task EachExecutionHasItsOwnBudget()
    {
        var (room, user, counter) = Ring(2);

        await room.StepAsync(user, 3, 3);
        await room.StepAsync(user, 2, 3);
        await room.StepAsync(user, 3, 3);

        // the same pulse, a second execution: another 200
        Assert.Equal(400, room.Wired.VariableValue(counter, WiredHolder.Room, null));
    }

    [Fact]
    public async Task ContextValuesReachTheStacksASignalRuns()
    {
        var room = new Room();
        var flag = Variable(room, "wf_var_context", new FurniWired { StringParam = "flag", IntParams = [1] });
        var plate = room.Furni(3, 1, "plate");
        var antenna = Antenna(room, 0, 3);
        var lamp = room.Furni(6, 1);

        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(1, 1, "wf_xtra_exec_in_order", Addon, new FurniWired());
        room.Box(1, 1, "wf_act_give_var", Effect,
            new FurniWired { VariableIds = [flag], IntParams = [WiredVariableTargets.Context, 0, 7, 0] });
        room.Box(1, 1, "wf_act_send_signal", Effect, Picks(antenna));

        // the receiver only sees the value as part of the sender's execution (Habbo: p38_limits)
        room.Box(5, 0, "wf_trg_recv_signal", Trigger, Picks(antenna));
        room.Box(5, 0, "wf_cnd_has_var", Condition, new FurniWired { VariableIds = [flag], IntParams = [WiredVariableTargets.Context] });
        room.Box(5, 0, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal("1", lamp.Data.State);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public async Task ASignalsReceiversDecideOnTheRoomAsItWasWhenItWasSent(int aX, int bX)
    {
        // Habbo's p35_flat: X adds one to cnt and signals Y; on Y, A signals X again while cnt is under 5, and B adds
        // one to num when cnt is 5. Were each receiver to decide after the one before had finished, every unwinding
        // level would see cnt at 5 and num would end at 5; on Habbo it ends at 1. Both orders of A and B are tried.
        var room = new Room();
        var cnt = Variable(room, "wf_var_room", new FurniWired { StringParam = "cnt", IntParams = [1] });
        var num = Variable(room, "wf_var_room", new FurniWired { StringParam = "num", IntParams = [1] });
        var x = Antenna(room, 4, 3);
        var y = Antenna(room, 5, 3);
        var plate = room.Furni(3, 3, "plate");

        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_act_send_signal", Effect, Picks(x));

        room.Box(0, 0, "wf_trg_recv_signal", Trigger, Picks(x));
        room.Box(0, 0, "wf_xtra_exec_in_order", Addon, new FurniWired());
        room.Box(0, 0, "wf_act_change_var_val", Effect, AddOne(cnt));
        room.Box(0, 0, "wf_act_send_signal", Effect, Picks(y));

        room.Box(aX, 0, "wf_trg_recv_signal", Trigger, Picks(y));
        room.Box(aX, 0, "wf_cnd_var_val_match", Condition, RoomValueIs(cnt, WiredComparison.LowerThan, 5));
        room.Box(aX, 0, "wf_act_send_signal", Effect, Picks(x));

        room.Box(bX, 0, "wf_trg_recv_signal", Trigger, Picks(y));
        room.Box(bX, 0, "wf_cnd_var_val_match", Condition, RoomValueIs(cnt, WiredComparison.EqualTo, 5));
        room.Box(bX, 0, "wf_act_change_var_val", Effect, AddOne(num));
        var user = room.User(2, 3);

        await room.StepAsync(user, 3, 3);

        Assert.Equal(5, room.Wired.VariableValue(room.Wired.FindVariable(cnt)!, WiredHolder.Room, null));
        Assert.Equal(1, room.Wired.VariableValue(room.Wired.FindVariable(num)!, WiredHolder.Room, null));
    }

    [Fact]
    public async Task OneExecutionMovesAtMostEightyFurni()
    {
        var room = new Room();
        var plate = room.Furni(3, 3, "plate");
        for (var i = 0; i < 100; i++)
            room.Furni(4 + i % 4, i % 3);
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        // raise everything in the room a little: altitude writes count as moves
        room.Box(0, 3, "wf_act_set_altitude", Effect, new FurniWired { IntParams = [10, 0], FurniSources = [WiredSources.AllFurni] });
        var before = room.Items.Items.ToDictionary(item => item, item => item.Z);
        var user = room.User(2, 3);

        await room.StepAsync(user, 3, 3);

        Assert.Equal(WiredExecution.MaxMoves, before.Count(entry => entry.Key.Z != entry.Value));
    }

    /// <summary>A stack that runs the bot box when the user walks onto the plate at (3, 1).</summary>
    private static (Room Room, RoomEntityStore.RoomEntity User, RoomEntityStore.RoomEntity Bot) BotStack(string className, FurniWired settings,
        Action<Room>? setUp = null)
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var bot = room.Bot(6, 3, "Frank");
        setUp?.Invoke(room);
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, className, Effect, settings);
        return (room, room.User(2, 1), bot);
    }

    [Fact]
    public async Task ABotTalksOrShoutsWhatItsBoxSays()
    {
        // the box's text is the bot's name, a tab, the message; Habbo's new box talks in the room's bubble width
        var (room, user, bot) = BotStack("wf_act_bot_talk", new FurniWired { StringParam = "frank\tHello room", IntParams = [1, 2] });

        await room.StepAsync(user, 3, 1);

        var said = Assert.Single(room.Said);
        Assert.Equal((bot, "Hello room", Habbo.Silo.Abstractions.Chat.RoomChatKind.Shout, 2), (said.Bot, said.Text, said.Kind, said.Width));
        Assert.Null(said.Listeners);
    }

    [Fact]
    public async Task ABotNamedOtherwiseSaysNothing()
    {
        var (room, user, _) = BotStack("wf_act_bot_talk", new FurniWired { StringParam = "Bob\tHello room", IntParams = [0, -1] });

        await room.StepAsync(user, 3, 1);

        Assert.Empty(room.Said);
    }

    [Fact]
    public async Task ABotWhispersToItsUsers()
    {
        var (room, user, bot) = BotStack("wf_act_bot_talk_to_avatar", new FurniWired { StringParam = "Frank\tpsst", IntParams = [1, -1] });

        await room.StepAsync(user, 3, 1);

        var said = Assert.Single(room.Said);
        Assert.Equal(Habbo.Silo.Abstractions.Chat.RoomChatKind.Whisper, said.Kind);
        Assert.Equal([user], said.Listeners!);
    }

    [Fact]
    public async Task ABotTeleportsOntoItsFurni()
    {
        var settings = new FurniWired { StringParam = "Frank" };
        var (room, user, bot) = BotStack("wf_act_bot_teleport", settings, r => settings.StuffIds.Add(r.Furni(5, 0, "rug").Item.Id));

        await room.StepAsync(user, 3, 1);

        Assert.Equal((5, 0), (bot.X, bot.Y));
    }

    [Fact]
    public async Task AHandItemGoesStraightToTheUsersWhenTheBoxNamesNoBot()
    {
        var (room, user, _) = BotStack("wf_act_bot_give_handitem", new FurniWired { StringParam = "", IntParams = [5] });

        await room.StepAsync(user, 3, 1);

        Assert.Equal(5, user.HandItem);
    }

    [Fact]
    public async Task ABotBringsTheHandItemOver()
    {
        var (room, user, bot) = BotStack("wf_act_bot_give_handitem", new FurniWired { StringParam = "Frank", IntParams = [5] });

        await room.StepAsync(user, 3, 1);

        Assert.Equal(5, user.HandItem);
        Assert.Equal((2, 1), (bot.X, bot.Y));
    }

    [Fact]
    public async Task ABotFollowsTheUserAndStops()
    {
        var (room, user, bot) = BotStack("wf_act_bot_follow_avatar", new FurniWired { StringParam = "Frank", IntParams = [1] });

        await room.StepAsync(user, 3, 1);
        Assert.Same(user, bot.Bot!.Following);

        room.Box(1, 3, "wf_trg_walks_off_furni", Trigger, Picks(room.Items.Items.First(i => i.Item.Definition!.ClassName == "plate")));
        room.Box(1, 3, "wf_act_bot_follow_avatar", Effect, new FurniWired { StringParam = "Frank", IntParams = [0] });
        await room.StepAsync(user, 2, 1);
        Assert.Null(bot.Bot.Following);
    }

    [Fact]
    public async Task ABotPutsOnTheBoxsLook()
    {
        var (room, user, bot) = BotStack("wf_act_bot_clothes", new FurniWired { StringParam = "Frank\thr-115-42.hd-190-1" });

        await room.StepAsync(user, 3, 1);

        Assert.Equal("hr-115-42.hd-190-1", bot.Bot!.Data.Figure);
    }

    [Fact]
    public async Task ABotReachingItsFurniOrAUserSetsOffItsTriggers()
    {
        var room = new Room();
        var rug = room.Furni(5, 1, "rug");
        var lamp = room.Furni(7, 0);
        var other = room.Furni(7, 1, "other");
        room.Box(0, 3, "wf_trg_bot_reached_stf", Trigger, new FurniWired { StringParam = "Frank", StuffIds = [rug.Item.Id] });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        room.Box(1, 3, "wf_trg_bot_reached_avtr", Trigger, new FurniWired { StringParam = "Frank" });
        room.Box(1, 3, "wf_act_toggle_state", Effect, Picks(other));
        var bot = room.Bot(5, 1, "Frank");

        await room.Wired.BotArrivedAsync(bot);
        Assert.Equal(("1", "0"), (lamp.Data.State, other.Data.State));

        room.User(4, 2);
        await room.Wired.BotArrivedAsync(bot);
        Assert.Equal(("0", "1"), (lamp.Data.State, other.Data.State));
    }

    /// <summary>A place furni box that copies the lamp from its snapshot when the user walks onto the plate.</summary>
    private static (Room Room, RoomEntityStore.RoomEntity User, RoomItemStore.PlacedItem Lamp, RoomItemStore.PlacedItem Plate) Placing(
        List<int> intParams, Action<FurniWired>? more = null)
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var lamp = room.Furni(6, 1);
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        var settings = new FurniWired
        {
            StuffIds = [lamp.Item.Id],
            IntParams = intParams,
            Snapshots = [new FurniWiredSnapshot { ItemId = lamp.Item.Id, State = "1", X = 6, Y = 1, Z = 0, Rotation = 2 }],
        };
        more?.Invoke(settings);
        room.Box(0, 3, "wf_act_place_furni", Effect, settings);
        return (room, room.User(2, 1), lamp, plate);
    }

    private static List<RoomItemStore.PlacedItem> Copies(Room room) => room.Items.Items.Where(item => item.Temporary).ToList();

    [Fact]
    public async Task PlaceFurniCopiesWhatItsSnapshotSawWhereItStood()
    {
        // Habbo (vault_zero, temporary_snapshot_copy): source snapshot, its location, its altitude; the original had moved
        var (room, user, lamp, _) = Placing([0, 0, 1, 0, 0, 0, 0, 0, 0, -20]);
        room.Items.MoveTo(lamp, 7, 0, 0, null);

        await room.StepAsync(user, 3, 1);

        var copy = Assert.Single(Copies(room));
        Assert.Equal((6, 1, 2, "1"), (copy.X, copy.Y, copy.Rotation, copy.Data.State));
        Assert.True(copy.Item.Id >= RoomItemStore.FirstTemporaryId);
        Assert.Equal((7, 0), (lamp.X, lamp.Y));
    }

    [Fact]
    public async Task PlaceFurniGoesOntoItsCustomTargetMovedByTheOffsets()
    {
        // custom location onto the triggering furni (the plate at 3, 1), one tile right
        var (room, user, _, _) = Placing([0, 1, 0, 1, 0, 0, 0, 0, 0, -20]);

        await room.StepAsync(user, 3, 1);

        var copy = Assert.Single(Copies(room));
        Assert.Equal((4, 1), (copy.X, copy.Y));
    }

    [Fact]
    public async Task APlacedCopyMayBeGivenAVariable()
    {
        // spawn with a variable, given the constant 42
        var (room, user, _, _) = Placing([0, 0, 1, 0, 0, 0, 1, 0, 42, -20]);
        var rank = Variable(room, "wf_var_furni", new FurniWired { StringParam = "rank", IntParams = [1, 1] });
        var box = room.Wired.StackAt(0, 3)!.Single(b => b.Type is Habbo.Silo.Wired.Effects.PlaceFurniEffect);
        box.Settings.VariableIds.AddRange([rank, ""]);

        await room.StepAsync(user, 3, 1);

        var copy = Assert.Single(Copies(room));
        Assert.Equal(42, room.Wired.VariableValue(room.Wired.FindVariable(rank)!, WiredHolder.Of(copy), null));
    }

    [Fact]
    public async Task RemoveFurniTakesAwayOnlyTemporaryCopies()
    {
        // Habbo (vault_zero, temporary_selection_cleanup): with the selector's furni, only the copies went
        var (room, user, lamp, plate) = Placing([0, 0, 1, 0, 0, 0, 0, 0, 0, -20]);
        await room.StepAsync(user, 3, 1);
        Assert.Single(Copies(room));

        room.Box(1, 3, "wf_trg_walks_off_furni", Trigger, Picks(plate));
        room.Box(1, 3, "wf_slc_furni_bytype", Selector, new FurniWired { StuffIds = [lamp.Item.Id], FurniSources = [WiredSources.PickedFurni] });
        room.Box(1, 3, "wf_act_remove_furni", Effect, new FurniWired());
        await room.StepAsync(user, 2, 1);

        Assert.Empty(Copies(room));
        Assert.Same(lamp, room.Wired.FindItem(lamp.Item.Id));
    }

    [Fact]
    public async Task ClickSettingsGoToTheUsersClient()
    {
        var (room, user, _) = BotStack("wf_act_click_conf", new FurniWired { IntParams = [2, 1] });

        await room.StepAsync(user, 3, 1);

        Assert.Contains(room.Sent, sent => sent.To == user.Player.Id
            && sent.Message is Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredClickSettingsMessageComposer { UserOption: 2, FurniOption: 1 });
    }

    [Fact]
    public async Task AUserIsSentThroughAPickedTeleport()
    {
        RoomItemStore.PlacedItem teleport = null!;
        var settings = new FurniWired();
        var (room, user, _) = BotStack("wf_act_teleport_to_room", settings, r =>
        {
            // a teleport's interaction comes from its definition as it enters the room
            var definition = new ItemDefinition { Id = 991, ClassName = "teleport", Logic = "furniture_multistate", Interaction = "teleport" };
            teleport = r.Items.Add(new UserItem { Id = 900, Definition = definition, DefinitionId = definition.Id }, FurniDataView.WithState("0"), 6, 0, 0);
            var rug = r.Furni(5, 0, "rug");
            settings.StuffIds.AddRange([teleport.Item.Id, rug.Item.Id]);
        });

        await room.StepAsync(user, 3, 1);

        Assert.Equal([(user, teleport)], room.SentThrough);
    }

    [Fact]
    public async Task FurniMoveTogetherKeepingTheirDistances()
    {
        // the group's top left corner goes to the target, here one right of the triggering user
        RoomItemStore.PlacedItem a = null!, b = null!;
        var settings = new FurniWired { IntParams = [1, 1, 0] };
        var (room, user, _) = BotStack("wf_act_move_furni_as_group", settings, r =>
        {
            a = r.Furni(5, 0, "a");
            b = r.Furni(6, 1, "b");
            settings.StuffIds.AddRange([a.Item.Id, b.Item.Id]);
        });

        await room.StepAsync(user, 3, 1);

        Assert.Equal(((4, 1), (5, 2)), ((a.X, a.Y), (b.X, b.Y)));
    }

    [Fact]
    public async Task ATriggerCanListenToWhatItsSelectorsChoose()
    {
        // walk on: by default its picks; set to its selectors', any plate of the picked plate's kind
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var other = room.Furni(5, 1, "plate");
        var lamp = room.Furni(7, 0);
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, new FurniWired { FurniSources = [WiredSources.SelectedFurni] });
        room.Box(0, 3, "wf_slc_furni_bytype", Selector, new FurniWired { StuffIds = [plate.Item.Id], FurniSources = [WiredSources.PickedFurni] });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(4, 1);

        // the other plate was never picked
        await room.StepAsync(user, other.X, other.Y);
        Assert.Equal("1", lamp.Data.State);

        // off it, onto bare floor: nothing
        await room.StepAsync(user, 6, 1);
        Assert.Equal("1", lamp.Data.State);
    }

    /// <summary>A stack moving the ball one tile east, from (4, 0) to (5, 0), with the add-on given: the options the move
    /// went with, and the ball.</summary>
    private static async Task<(WiredMoveOptions Options, RoomItemStore.PlacedItem Ball)> MovedWith(string addon, FurniWired settings)
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var ball = room.Furni(4, 0, "ball");
        ball.Item.Definition!.Directions = [new ItemDefinitionDirection { Direction = 0 }, new ItemDefinitionDirection { Direction = 2 }];
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, addon, Addon, settings);
        var move = Picks(ball);
        move.IntParams = [5, 0]; // east
        room.Box(0, 3, "wf_act_move_rotate", Effect, move);
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        return (Assert.Single(room.MoveOptions), ball);
    }

    [Fact]
    public async Task AMovementCurveMakesTheMoveArc()
    {
        var (options, _) = await MovedWith("wf_xtra_mov_curve", new FurniWired { IntParams = [0, 160, -20] });

        Assert.Equal(160, options.CurveStrength);
    }

    [Fact]
    public async Task AProjectileFacesWhereItFliesOverItsTimePerTileAndOvershoots()
    {
        // turned east (2), 300 ms a tile over one tile, overshooting by two tiles of that, curved 50
        var (options, ball) = await MovedWith("wf_xtra_rotate_to_dir",
            new FurniWired { IntParams = [1, 0, 1, 0, 300, -20, 1, 1, 0, 4, 0, 0, 1, 1, 1, 0, 2, -20, 50] });

        Assert.Equal((5, 0, 2), (ball.X, ball.Y, ball.Rotation));
        Assert.Equal((300, 600, 50), (options.AnimationMilliseconds, options.OvershootMilliseconds, options.CurveStrength));
    }

    [Fact]
    public async Task WhenAConditionFailsTheNegativeEffectsRunInstead()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var mat = room.Furni(5, 3, "mat");
        var lamp = room.Furni(6, 1);
        var other = room.Furni(7, 1);
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(1, 1, "wf_cnd_trggrer_on_frn", Condition, Picks(mat));
        room.Box(1, 1, "wf_act_toggle_state", Effect, Picks(lamp));
        room.Box(1, 1, "wf_act_neg_call_stacks", Effect, new FurniWired());
        var elseBox = room.Box(4, 3, "wf_act_toggle_state", Effect, Picks(other));
        room.Wired.Add(room.Items.Find(room.Items.Items.Last(i => i.Item.Definition!.ClassName == "wf_act_neg_call_stacks").Item.Id)!,
            new FurniWired { ItemId = room.Items.Items.Last(i => i.Item.Definition!.ClassName == "wf_act_neg_call_stacks").Item.Id, StuffIds = [elseBox.Item.Id] });
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal("0", lamp.Data.State);
        Assert.Equal("1", other.Data.State);
    }

    [Fact]
    public async Task JoiningATeamDressesTheUserAndScoresShowOnTheirColoursBoard()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var board = room.Furni(7, 0, "bb_score_r", logic: "furniture_score");
        board.Item.Definition!.CustomParams = "1";
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(1, 1, "wf_xtra_exec_in_order", Addon, new FurniWired());
        room.Box(1, 1, "wf_act_join_team", Effect, new FurniWired { IntParams = [1, 0] });
        room.Box(1, 1, "wf_act_give_score", Effect, new FurniWired { IntParams = [5, 0] });
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal(1, user.Team);
        Assert.Equal(223, user.EffectId);
        Assert.Equal(5, room.Games.Score(1));
        Assert.Equal("5", board.Data.State);
    }

    [Fact]
    public async Task ReachingAScoreFiresTheScoreTriggerOnce()
    {
        var room = new Room();
        var lamp = room.Furni(6, 1);
        room.Box(1, 1, "wf_trg_score_achieved", Trigger, new FurniWired { IntParams = [10, 2] });
        room.Box(1, 1, "wf_act_toggle_state", Effect, Picks(lamp));

        await room.Games.AddScoreAsync(2, 6);
        Assert.Equal("0", lamp.Data.State);
        await room.Games.AddScoreAsync(2, 6);
        Assert.Equal("1", lamp.Data.State);
        await room.Games.AddScoreAsync(2, 6);
        Assert.Equal("1", lamp.Data.State);
    }

    [Theory]
    [InlineData(WiredQuantity.Any, "1")]
    [InlineData(WiredQuantity.All, "0")]
    public async Task ATeamScoreWithNoTeamChosenCountsItsUsersTeams(int quantifier, string lampAfter)
    {
        // the selector's users: one on team 1 (5 points), one on team 2 (none); "has 5 points" holds for any, not all
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var lamp = room.Furni(7, 0);
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_slc_users_bytype", Selector, new FurniWired { IntParams = [WiredUserKinds.Habbo] });
        room.Box(0, 3, "wf_cnd_team_has_score", Condition, new FurniWired
        {
            IntParams = [WiredGames.AnyOrTriggererTeam, 5, WiredGames.EqualTo],
            UserSources = [WiredSources.SelectedUsers],
            Quantifier = quantifier,
        });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var red = room.User(2, 1, "red");
        var green = room.User(6, 3, "green");
        red.Team = 1;
        green.Team = 2;
        await room.Games.AddScoreAsync(1, 5);

        await room.StepAsync(red, 3, 1);

        Assert.Equal(lampAfter, lamp.Data.State);
    }

    [Fact]
    public async Task StartingAResetClockStartsTheGameAndItsCountdownEndsIt()
    {
        var room = new Room();
        var lamp = room.Furni(6, 1);
        var other = room.Furni(7, 1);
        var clock = room.Furni(0, 3, "bb_counter", logic: "furniture_counter_clock");
        clock.Item.Definition!.CustomParams = "1,5";
        room.Box(1, 1, "wf_trg_game_starts", Trigger, new FurniWired());
        room.Box(1, 1, "wf_act_toggle_state", Effect, Picks(lamp));
        room.Box(2, 2, "wf_trg_game_ends", Trigger, new FurniWired());
        room.Box(2, 2, "wf_act_toggle_state", Effect, Picks(other));
        await room.Games.AddScoreAsync(1, 3);

        await room.Games.UseClockAsync(clock, 1);

        Assert.True(room.Games.Running);
        Assert.Equal(0, room.Games.Score(1));
        Assert.Equal("1", lamp.Data.State);
        Assert.Equal("1", clock.Data.State);

        await room.Games.PulseAsync();
        await room.Games.PulseAsync();

        Assert.False(room.Games.Running);
        Assert.Equal("0", clock.Data.State);
        Assert.Equal("1", other.Data.State);
    }

    [Fact]
    public void AKeywordTriggerThatHidesTellsTheRoomBeforeTheMessageGoesOut()
    {
        var room = new Room();
        room.Box(1, 1, "wf_trg_says_something", Trigger, new FurniWired { StringParam = "open sesame", IntParams = [0, 0, 1] });
        var user = room.User(2, 1);

        Assert.True(room.Wired.Hides(user, "please OPEN SESAME now"));
        Assert.False(room.Wired.Hides(user, "open the door"));
    }

    [Fact]
    public void ANewBoxOpensWithItsDefaults()
    {
        var room = new Room();
        var box = room.Box(1, 1, "wf_act_move_rotate", Effect, new FurniWired());

        var dialog = Assert.IsType<WiredFurniActionComposer>(room.Wired.DialogFor(box.Item.Id));

        Assert.Equal(box.Item.Id, dialog.Box.ItemId);
        Assert.Equal(EffectCodes.MoveFurni, dialog.Box.Code);
        // as Habbo opens a new one (bc_run4): its own parameters, and the same to reset to
        Assert.Equal([0, 0, 1], dialog.Box.IntParams);
        Assert.Equal([0, 0, 1], dialog.Context.DefaultIntParams);
        Assert.Equal(WiredBoxType.MaxPickedFurni, dialog.Box.FurniLimit);
    }

    [Fact]
    public void EveryBoxTypeIsNamedForItsFamilyAndHasItsOwnCode()
    {
        var prefixes = new Dictionary<WiredKind, string>
        {
            [WiredKind.Trigger] = "wf_trg_",
            [WiredKind.Effect] = "wf_act_",
            [WiredKind.Condition] = "wf_cnd_",
            [WiredKind.Selector] = "wf_slc_",
            [WiredKind.Addon] = "wf_xtra_",
            [WiredKind.Variable] = "wf_var_",
        };
        var types = Boxes.ClassNames.Select(name => (Name: name, Type: Boxes.For(name)!)).ToList();

        Assert.All(types, t => Assert.StartsWith(prefixes[t.Type.Kind], t.Name));
        Assert.All(types.GroupBy(t => t.Type.Kind), family =>
        {
            // a negative condition opens its positive's dialog
            var codes = family.Select(t => t.Type).Distinct().GroupBy(type => (type.Code, (type as WiredCondition)?.IsNegative));
            Assert.All(codes, code => Assert.Single(code));
        });
    }

    [Fact]
    public async Task ChasingFurniStepsTowardsTheUserAndCatchesThem()
    {
        var room = new Room();
        var ghost = room.Furni(0, 1, "ghost");
        var lamp = room.Furni(7, 3);
        room.Box(1, 3, "wf_trg_periodically", Trigger, new FurniWired { IntParams = [1] });
        room.Box(1, 3, "wf_act_chase", Effect, Picks(ghost));
        room.Box(6, 3, "wf_trg_collision", Trigger, new FurniWired());
        room.Box(6, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        room.User(3, 1);

        await room.Wired.PulseAsync();
        Assert.Equal((1, 1), (ghost.X, ghost.Y));
        await room.Wired.PulseAsync();
        Assert.Equal((2, 1), (ghost.X, ghost.Y));
        Assert.Equal("0", lamp.Data.State);

        await room.Wired.PulseAsync();
        Assert.Equal((2, 1), (ghost.X, ghost.Y));
        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task ChasingFurniGoesForTheUserInItsRowOrColumn()
    {
        // Habbo: "towards nearest user in the same row or column"; one nearer but diagonal does not count
        var room = new Room();
        var ghost = room.Furni(5, 1, "ghost");
        room.Box(7, 3, "wf_trg_periodically", Trigger, new FurniWired { IntParams = [1] });
        room.Box(7, 3, "wf_act_chase", Effect, Picks(ghost));
        room.User(2, 1);
        room.User(6, 2);

        await room.Wired.PulseAsync();

        Assert.Equal((4, 1), (ghost.X, ghost.Y));
    }

    [Fact]
    public async Task ATeleportOntoFurniNoOneCanStandOnLandsBesideIt()
    {
        // Habbo (destination_geometry): onto a blocking antenna at (14, 11) the avatar landed on (14, 10)
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var table = room.Furni(5, 2, "table");
        table.Item.Definition!.CanWalk = false;
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_act_teleport_to", Effect, Picks(table));
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);
        for (var pulse = 0; pulse < 4; pulse++)
            await room.Wired.PulseAsync();

        Assert.Equal((5, 1), (user.X, user.Y));
    }

    [Fact]
    public async Task FleeingFurniStepsAwayFromTheUser()
    {
        var room = new Room();
        var mouse = room.Furni(3, 1, "mouse");
        room.Box(7, 3, "wf_trg_periodically", Trigger, new FurniWired { IntParams = [1] });
        room.Box(7, 3, "wf_act_flee", Effect, Picks(mouse));
        room.User(1, 1);

        await room.Wired.PulseAsync();

        Assert.Equal((4, 1), (mouse.X, mouse.Y));
    }

    [Fact]
    public async Task FurniMovingInADirectionTurnsWhenBlocked()
    {
        var room = new Room();
        var ball = room.Furni(6, 0, "ball");
        room.Box(0, 3, "wf_trg_periodically", Trigger, new FurniWired { IntParams = [1] });
        var move = Picks(ball);
        move.IntParams = [2, 5, 0]; // east, and back when blocked
        room.Box(0, 3, "wf_act_move_to_dir", Effect, move);

        await room.Wired.PulseAsync();
        Assert.Equal((7, 0), (ball.X, ball.Y));
        await room.Wired.PulseAsync(); // the wall: it turns back
        Assert.Equal((7, 0), (ball.X, ball.Y));
        await room.Wired.PulseAsync();
        Assert.Equal((6, 0), (ball.X, ball.Y));
    }

    [Fact]
    public async Task FurniMovesBesideAnotherFurni()
    {
        var room = new Room();
        var ball = room.Furni(0, 0, "ball");
        var goal = room.Furni(4, 2, "goal");
        room.Box(7, 3, "wf_trg_periodically", Trigger, new FurniWired { IntParams = [1] });
        room.Box(7, 3, "wf_act_move_furni_to", Effect,
            new FurniWired { StuffIds = [ball.Item.Id], StuffIds2 = [goal.Item.Id], IntParams = [2, 2] });

        await room.Wired.PulseAsync();

        Assert.Equal((6, 2), (ball.X, ball.Y));
    }

    [Fact]
    public async Task FurniMovesOntoAnotherFurni()
    {
        var room = new Room();
        var cup = room.Furni(0, 0, "cup");
        var table = room.Furni(3, 3, "table");
        room.Box(7, 3, "wf_trg_periodically", Trigger, new FurniWired { IntParams = [1] });
        room.Box(7, 3, "wf_act_furni_to_furni", Effect, new FurniWired
        {
            StuffIds = [cup.Item.Id],
            StuffIds2 = [table.Item.Id],
            FurniSources = [WiredSources.PickedFurni, WiredSources.PickedFurni2],
        });

        await room.Wired.PulseAsync();

        Assert.Equal((3, 3), (cup.X, cup.Y));
        Assert.True(cup.Z > table.Z);
    }

    [Fact]
    public async Task FurniMovesRelativelyAndToTheTriggeringUser()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var box = room.Furni(0, 0, "box");
        var crate = room.Furni(2, 3, "crate");
        room.Box(7, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(7, 3, "wf_act_rel_mov", Effect, new FurniWired { StuffIds = [crate.Item.Id], IntParams = [2, -1] });
        room.Box(7, 3, "wf_act_furni_to_user", Effect, new FurniWired { StuffIds = [box.Item.Id] });
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal((4, 2), (crate.X, crate.Y));
        Assert.Equal((3, 1), (box.X, box.Y));
    }

    [Fact]
    public async Task AltitudeRises()
    {
        var room = new Room();
        var lamp = room.Furni(4, 1);
        room.Box(7, 3, "wf_trg_periodically", Trigger, new FurniWired { IntParams = [1] });
        room.Box(7, 3, "wf_act_set_altitude", Effect, new FurniWired { StuffIds = [lamp.Item.Id], IntParams = [150, 0] });

        await room.Wired.PulseAsync();
        Assert.Equal(1.5, lamp.Z, 2);
        await room.Wired.PulseAsync();
        Assert.Equal(3.0, lamp.Z, 2);
    }

    [Fact]
    public async Task UsersAreMovedAndTurned()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_act_move_rotate_user", Effect, new FurniWired { IntParams = [3, 9] }); // south-east, turned clockwise
        var user = room.User(2, 1);
        user.BodyDirection = 2;

        await room.StepAsync(user, 3, 1);

        Assert.Equal((4, 2), (user.X, user.Y));
        Assert.Equal(3, user.BodyDirection);
    }

    [Fact]
    public async Task UsersAreSentToFurni()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var mat = room.Furni(6, 3, "mat");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_act_user_to_furni", Effect, new FurniWired { StuffIds = [mat.Item.Id], IntParams = [2] });
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal((6, 3), (user.X, user.Y));
    }

    [Fact]
    public async Task AFrozenUserCannotWalkUntilTeleported()
    {
        var room = new Room();
        room.Texts["wiredfurni.params.freeze.effect.1"] = "${fx_218}";
        var plate = room.Furni(3, 1, "plate");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_act_freeze", Effect, new FurniWired { IntParams = [1, 1] });
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);
        Assert.True(user.Frozen);
        Assert.Equal(218, user.EffectId);
        Assert.False(room.Entities.Walk(user, 5, 1));

        room.Wired.WarpUser(user, 6, 3);
        Assert.False(user.Frozen);
        Assert.Equal(0, user.EffectId);
        Assert.True(room.Entities.Walk(user, 5, 1));
    }

    [Fact]
    public async Task KickAndMuteTheTriggeringUser()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_xtra_exec_in_order", Addon, new FurniWired());
        room.Box(0, 3, "wf_act_mute_triggerer", Effect, new FurniWired { IntParams = [5], StringParam = "shh" });
        room.Box(0, 3, "wf_act_kick_user", Effect, new FurniWired { StringParam = "bye" });
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.True(user.MutedUntil > DateTime.UtcNow);
        Assert.Equal([user], room.Kicked);
    }

    [Fact]
    public async Task ClickingAPickedFurniFiresTheClickTrigger()
    {
        var room = new Room();
        var button = room.Furni(3, 1, "button");
        var lamp = room.Furni(6, 1);
        room.Box(0, 3, "wf_trg_click_furni", Trigger, Picks(button));
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        await room.Wired.FurniClickedAsync(user, lamp);
        Assert.Equal("0", lamp.Data.State);
        await room.Wired.FurniClickedAsync(user, button);
        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task TheClickTileTriggerHearsOnlyClickTiles()
    {
        var room = new Room();
        var tile = room.Furni(3, 1, "room_invisible_click_tile", ClickTileInteraction.Name);
        var lamp = room.Furni(6, 1);
        var trigger = Boxes.For("wf_trg_click_tile")!;
        room.Box(0, 3, "wf_trg_click_tile", Trigger, Picks(tile));
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        Assert.True(trigger.PickRule!.Accepts(tile));
        Assert.False(trigger.PickRule.Accepts(lamp));
        await room.Wired.FurniClickedAsync(user, tile);
        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task ClickingAUserFiresTheTriggerAndMayKeepTheMenuShut()
    {
        var room = new Room();
        var lamp = room.Furni(6, 1);
        var clicker = room.User(2, 1);
        var clicked = room.User(4, 1, "clicked");

        Assert.Equal((true, true), await room.Wired.UserClickedAsync(clicker, clicked));
        Assert.False(Assert.IsType<WiredEnvironmentMessageComposer>(room.Wired.Environment()).HasClickUserWired);

        room.Box(0, 3, "wf_trg_click_user", Trigger, new FurniWired { IntParams = [1, 0] });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));

        Assert.Contains(room.Broadcasts, m => m is WiredEnvironmentMessageComposer { HasClickUserWired: true });
        Assert.Equal((false, true), await room.Wired.UserClickedAsync(clicker, clicked));
        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task LeavingTheRoomFiresTheLeaveTrigger()
    {
        var room = new Room();
        var lamp = room.Furni(6, 1);
        room.Box(0, 3, "wf_trg_leave_room", Trigger, new FurniWired());
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        room.Entities.Leave(user.Player.Id);
        await room.Wired.UserLeftAsync(user);

        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task AStateChangeFiresForEveryStateOrOnlyTheSavedOne()
    {
        var room = new Room();
        var lever = room.Furni(3, 1, "lever");
        var every = room.Furni(6, 1, "every");
        var saved = room.Furni(6, 2, "saved");
        room.Box(0, 3, "wf_trg_stuff_state", Trigger, new FurniWired { StuffIds = [lever.Item.Id], IntParams = [0] });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(every));
        room.Box(1, 3, "wf_trg_stuff_state", Trigger, new FurniWired
        {
            StuffIds = [lever.Item.Id],
            IntParams = [1],
            Snapshots = [new FurniWiredSnapshot { ItemId = lever.Item.Id, State = "0" }],
        });
        room.Box(1, 3, "wf_act_toggle_state", Effect, Picks(saved));

        await room.Wired.SetStateAsync(lever, "1");
        Assert.Equal(("1", "0"), (every.Data.State, saved.Data.State));

        await room.Wired.SetStateAsync(lever, "0");
        Assert.Equal(("0", "1"), (every.Data.State, saved.Data.State));
    }

    [Theory]
    [InlineData(WiredSources.PickedFurni, "0")]
    [InlineData(WiredSources.TriggerFurni, "1")]
    public async Task MatchSnapshotRestoresOnlyWhatItsSourceSupplies(int source, string lampAfter)
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var lamp = room.Furni(6, 1);
        lamp.Data = FurniDataView.WithState("1");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_act_match_to_sshot", Effect, new FurniWired
        {
            StuffIds = [lamp.Item.Id],
            IntParams = [1, 0, 0, 0],
            Snapshots = [new FurniWiredSnapshot { ItemId = lamp.Item.Id, State = "0", X = 6, Y = 1 }],
            FurniSources = [source],
        });
        var user = room.User(2, 1);

        // the trigger's furni is the plate, which has no snapshot
        await room.StepAsync(user, 3, 1);

        Assert.Equal(lampAfter, lamp.Data.State);
    }

    [Fact]
    public async Task TheShortRepeatTriggerCountsFiftyMillisecondTicks()
    {
        var room = new Room();
        var lamp = room.Furni(6, 1);
        Assert.False(room.Wired.HasShortTicks);
        room.Box(0, 3, "wf_trg_period_short", Trigger, new FurniWired { IntParams = [3] });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        Assert.True(room.Wired.HasShortTicks);

        for (var tick = 0; tick < 7; tick++)
            await room.Wired.ShortTickAsync();

        // ticks 1, 4 and 7
        Assert.Equal(3, room.StateUpdates);
    }

    [Fact]
    public void TheLongTimerIsTheSameBoxAsTheTimer() =>
        Assert.Same(Boxes.For("wf_trg_at_given_time"), Boxes.For("wf_trg_at_time_long"));

    /// <summary>A stack a walk onto the plate at (3, 1) runs, toggling the lamp when its conditions hold.</summary>
    private static (Room Room, RoomItemStore.PlacedItem Lamp, RoomEntityStore.RoomEntity User) Guarded(
        string condition, FurniWired settings, Action<Room>? setUp = null)
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var lamp = room.Furni(6, 1);
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, condition, Condition, settings);
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        setUp?.Invoke(room);
        return (room, lamp, room.User(2, 1));
    }

    [Fact]
    public void ANegativeConditionOpensItsPositivesDialogInverted()
    {
        var room = new Room();
        var box = room.Box(1, 1, "wf_cnd_not_in_team", Condition, new FurniWired());

        var dialog = Assert.IsType<WiredFurniConditionComposer>(room.Wired.DialogFor(box.Item.Id));

        // Habbo sends it under its own code, 17, which the client opens as the positive's form, inverted
        Assert.Equal(ConditionCodes.NotActorIsInTeam, dialog.Box.Code);
        Assert.True(dialog.IsInvert);
        Assert.Equal(WiredQuantifierTypes.Users, dialog.QuantifierType);
    }

    [Fact]
    public async Task TheQuantifierChoosesAllOrAnyUsers()
    {
        // two users, one wearing the effect: "all" fails, "any" passes
        foreach (var (quantifier, expected) in new[] { (WiredQuantity.All, "0"), (WiredQuantity.Any, "1") })
        {
            var (room, lamp, user) = Guarded("wf_cnd_wearing_effect",
                new FurniWired { IntParams = [5], Quantifier = quantifier, UserSources = [WiredSources.AllUsers] });
            user.EffectId = 5;
            room.User(6, 3, "other");

            await room.StepAsync(user, 3, 1);

            Assert.Equal(expected, lamp.Data.State);
        }
    }

    [Fact]
    public async Task ANegativeConditionPassesWhenItsPositiveWouldNot()
    {
        var (room, lamp, user) = Guarded("wf_cnd_not_wearing_fx", new FurniWired { IntParams = [5] });

        await room.StepAsync(user, 3, 1);
        Assert.Equal("1", lamp.Data.State);

        user.EffectId = 5;
        await room.StepAsync(user, 2, 1);
        await room.StepAsync(user, 3, 1);
        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task TheTriggererMatchesByName()
    {
        var (room, lamp, user) = Guarded("wf_cnd_triggerer_match",
            new FurniWired { IntParams = [WiredUserKinds.Habbo], StringParam = "TESTER" });

        await room.StepAsync(user, 3, 1);

        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task TheTriggererMatchesNoOneElseByName()
    {
        // the name is the second slot's source, "the user specified by name", Habbo's default for it
        var (room, lamp, _) = Guarded("wf_cnd_triggerer_match",
            new FurniWired { IntParams = [WiredUserKinds.Habbo], StringParam = "someone" });
        var other = room.User(1, 1, "other");

        await room.StepAsync(other, 3, 1);

        Assert.Equal("0", lamp.Data.State);
    }

    [Theory]
    [InlineData("plate", "1")]
    [InlineData("rug", "0")]
    public async Task StuffIsComparesTheTriggeringFurniWithThePicksKinds(string pickedKind, string lampAfter)
    {
        // Habbo's dialog: the furni to test (the triggering one by default), then the furni to compare with (the picks)
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var picked = room.Furni(7, 0, pickedKind);
        var lamp = room.Furni(6, 1);
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_cnd_stuff_is", Condition, Picks(picked));
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal(lampAfter, lamp.Data.State);
    }

    [Fact]
    public async Task ABoxNeverSavedRunsOnTheParametersHabboGivesANewOne()
    {
        // a new "repeat long" box on Habbo holds 10: every 10 times 5 seconds; never saved, it still counts that
        var room = new Room();
        var lamp = room.Furni(6, 1);
        room.Box(0, 3, "wf_trg_period_long", Trigger, new FurniWired());
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));

        // it fires on its first pulse, then every 100 (with one step of 5 seconds it would be every 10)
        await room.Wired.PulseAsync();
        Assert.Equal("1", lamp.Data.State);
        for (var pulse = 0; pulse < 99; pulse++)
            await room.Wired.PulseAsync();
        Assert.Equal("1", lamp.Data.State);

        await room.Wired.PulseAsync();
        Assert.Equal("0", lamp.Data.State);
    }

    [Theory]
    [InlineData("wf_cnd_has_handitem", 5, "1")]
    [InlineData("wf_cnd_has_handitem", 2, "0")]
    [InlineData("wf_cnd_not_has_handitem", 2, "1")]
    public async Task TheUserMustHoldTheHandItem(string condition, int holding, string lampAfter)
    {
        var (room, lamp, user) = Guarded(condition, new FurniWired { IntParams = [5] });
        user.HandItem = holding;

        await room.StepAsync(user, 3, 1);

        Assert.Equal(lampAfter, lamp.Data.State);
    }

    [Theory]
    [InlineData("", "1")]       // the room's group
    [InlineData("77", "0")]     // another group
    [InlineData("12", "1")]
    public async Task TheUserMustBeInTheGroup(string chosen, string lampAfter)
    {
        var (room, lamp, user) = Guarded("wf_cnd_actor_in_group", new FurniWired { StringParam = chosen });
        room.Wired.GroupId = 12;
        user.GroupIds = new HashSet<int> { 12 };

        await room.StepAsync(user, 3, 1);

        Assert.Equal(lampAfter, lamp.Data.State);
    }

    [Theory]
    [InlineData("wf_cnd_wearing_badge", "ACH_Login1", "1")]
    [InlineData("wf_cnd_wearing_badge", "ADM", "0")]
    [InlineData("wf_cnd_not_wearing_b", "ADM", "1")]
    public async Task TheUserMustWearTheBadge(string condition, string badge, string lampAfter)
    {
        var (room, lamp, user) = Guarded(condition, new FurniWired { StringParam = badge });
        user.WornBadges = ["ach_login1", "HC1"];

        await room.StepAsync(user, 3, 1);

        Assert.Equal(lampAfter, lamp.Data.State);
    }

    [Fact]
    public async Task SelectorsChooseUsersByHandItemAndGroup()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_slc_users_handitem", Selector, new FurniWired { IntParams = [7] });
        room.Box(0, 3, "wf_slc_users_group", Selector, new FurniWired { StringParam = "4", IsFilter = true });
        room.Box(0, 3, "wf_act_kick_user", Effect, new FurniWired { UserSources = [WiredSources.SelectedUsers] });
        var walker = room.User(2, 1, "walker");
        var holder = room.User(6, 3, "holder");
        var member = room.User(5, 3, "member");
        holder.HandItem = member.HandItem = 7;
        member.GroupIds = new HashSet<int> { 4 };

        await room.StepAsync(walker, 3, 1);

        Assert.Equal([member], room.Kicked);
    }

    [Fact]
    public async Task TheUserMustFaceAChosenDirection()
    {
        var (room, lamp, user) = Guarded("wf_cnd_actor_dir", new FurniWired { IntParams = [1 << 2] });

        await room.StepAsync(user, 3, 1); // warped: facing its door direction, east
        Assert.Equal(2, user.BodyDirection);
        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task FurniAltitudeIsCompared()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var lamp = room.Furni(6, 1);
        var high = room.Furni(5, 3, "high");
        high.Z = 0.5;
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_cnd_has_altitude", Condition,
            new FurniWired { StuffIds = [high.Item.Id], IntParams = [40, WiredComparison.HigherThan] });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task TheQuantityOfUsersIsCompared()
    {
        var (room, lamp, user) = Guarded("wf_cnd_slc_quantity",
            new FurniWired { IntParams = [1, 1, WiredComparison.HigherThan], UserSources = [WiredSources.AllUsers] });
        room.User(6, 3, "other");

        await room.StepAsync(user, 3, 1);

        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task ValidMovesChecksTheStacksPlannedMoves()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var ball = room.Furni(7, 0, "ball");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_cnd_valid_moves", Condition, new FurniWired());
        room.Box(0, 3, "wf_act_rel_mov", Effect, new FurniWired { StuffIds = [ball.Item.Id], IntParams = [1, 0] });
        room.Box(0, 3, "wf_act_neg_call_stacks", Effect, new FurniWired());
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1); // off the edge: the move is not valid, so nothing moves

        Assert.Equal((7, 0), (ball.X, ball.Y));

        var room2 = new Room();
        var plate2 = room2.Furni(3, 1, "plate");
        var ball2 = room2.Furni(5, 0, "ball");
        room2.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate2));
        room2.Box(0, 3, "wf_cnd_valid_moves", Condition, new FurniWired());
        room2.Box(0, 3, "wf_act_rel_mov", Effect, new FurniWired { StuffIds = [ball2.Item.Id], IntParams = [1, 0] });
        var user2 = room2.User(2, 1);

        await room2.StepAsync(user2, 3, 1);

        Assert.Equal((6, 0), (ball2.X, ball2.Y));
    }

    [Fact]
    public async Task TimeAndDateConditionsReadTheClockInTheBoxsZone()
    {
        // a Wednesday in March, 14:30:15 UTC
        var now = new DateTimeOffset(2026, 3, 18, 14, 30, 15, TimeSpan.Zero);

        var (room, lamp, user) = Guarded("wf_cnd_match_time",
            new FurniWired { IntParams = [0, 1, 1, 0, 0, 25, 35, 14, 14] });
        room.Wired.Clock = () => now;
        await room.StepAsync(user, 3, 1);
        Assert.Equal("1", lamp.Data.State);

        // 14:30 UTC is 15:30 in Copenhagen: the hour no longer matches
        var (room2, lamp2, user2) = Guarded("wf_cnd_match_time",
            new FurniWired { IntParams = [0, 0, 1, 0, 0, 0, 0, 14, 14], StringParam = "Europe/Copenhagen" });
        room2.Wired.Clock = () => now;
        await room2.StepAsync(user2, 3, 1);
        Assert.Equal("0", lamp2.Data.State);

        // Wednesday is the third weekday box, March the third month box
        var (room3, lamp3, user3) = Guarded("wf_cnd_match_date",
            new FurniWired { IntParams = [1, 1, 1 << 2, 10, 20, 1 << 2, 2026, 2026] });
        room3.Wired.Clock = () => now;
        await room3.StepAsync(user3, 3, 1);
        Assert.Equal("1", lamp3.Data.State);

        var (room4, lamp4, user4) = Guarded("wf_cnd_date_rng_active",
            new FurniWired { IntParams = [(int)now.AddDays(1).ToUnixTimeSeconds()] });
        room4.Wired.Clock = () => now;
        await room4.StepAsync(user4, 3, 1);
        Assert.Equal("0", lamp4.Data.State);
    }

    /// <summary>A stack a walk onto the plate at (3, 1) runs: the selector chooses, and the toggle
    /// switches what it chose.</summary>
    private static async Task<Room> SelectAndToggleAsync(string selector, Func<Room, FurniWired> settings, Action<Room> setUp)
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        setUp(room);
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, selector, Selector, settings(room));
        room.Box(0, 3, "wf_act_toggle_state", Effect, new FurniWired { FurniSources = [WiredSources.SelectedFurni] });
        var user = room.User(2, 1);
        await room.StepAsync(user, 3, 1);
        return room;
    }

    private static RoomItemStore.PlacedItem Named(Room room, string className) =>
        room.Items.Items.First(item => item.Item.Definition!.ClassName == className);

    [Fact]
    public async Task TheAreaSelectorChoosesFurniInItsRectangle()
    {
        var room = await SelectAndToggleAsync("wf_slc_furni_area",
            _ => new FurniWired { IntParams = [5, 0, 2, 2] },
            r => { r.Furni(5, 1, "inside"); r.Furni(7, 1, "outside"); });

        Assert.Equal("1", Named(room, "inside").Data.State);
        Assert.Equal("0", Named(room, "outside").Data.State);
    }

    [Fact]
    public async Task TheNeighborhoodIsDrawnAroundTheTriggeringFurni()
    {
        // the drawing's second tile, one east of its centre, is the only one marked
        var room = await SelectAndToggleAsync("wf_slc_furni_neighborhood",
            _ => new FurniWired { IntParams = [0, 0, 0, 1 << 1], FurniSources = [WiredSources.TriggerFurni] },
            r => { r.Furni(4, 1, "east"); r.Furni(2, 2, "elsewhere"); });

        Assert.Equal("1", Named(room, "east").Data.State);
        Assert.Equal("0", Named(room, "elsewhere").Data.State);
    }

    [Fact]
    public void TheNeighborhoodReadsTheClientsSpiral()
    {
        // ranks 0 (the centre), 1 (east) and 3 (north); the reference tile moved one west
        var room = new Room();
        var item = room.Furni(0, 3, "wf_slc_furni_neighborhood");
        var box = new WiredBox(item, Boxes.For("wf_slc_furni_neighborhood")!, new FurniWired { IntParams = [0, -1, 0, 0b1011] });

        var offsets = WiredTiles.Neighborhood(box);

        Assert.Equal([(1, 0), (2, 0), (1, -1)], offsets);
    }

    [Fact]
    public async Task FurniOnFurniChoosesWhatStandsAbove()
    {
        var room = await SelectAndToggleAsync("wf_slc_furni_onfurni",
            r => new FurniWired { StuffIds = [Named(r, "table").Item.Id], IntParams = [0] },
            r => { r.Furni(5, 2, "table"); r.Furni(5, 2, "cup").Z = 0.5; r.Furni(6, 2, "beside"); });

        Assert.Equal("1", Named(room, "cup").Data.State);
        Assert.Equal("0", Named(room, "table").Data.State);
        Assert.Equal("0", Named(room, "beside").Data.State);
    }

    [Fact]
    public async Task FurniAreChosenByAltitude()
    {
        var room = await SelectAndToggleAsync("wf_slc_furni_altitude",
            _ => new FurniWired { IntParams = [50, WiredComparison.EqualTo] },
            r => { r.Furni(5, 2, "table"); r.Furni(5, 2, "cup").Z = 0.5; });

        Assert.Equal("1", Named(room, "cup").Data.State);
        Assert.Equal("0", Named(room, "table").Data.State);
    }

    [Fact]
    public async Task UsersAreChosenInAnAreaAndByType()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var mat = room.Furni(6, 3, "mat");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_slc_users_area", Selector, new FurniWired { IntParams = [5, 2, 3, 2] });
        room.Box(0, 3, "wf_slc_users_bytype", Selector, new FurniWired { IntParams = [WiredUserKinds.Habbo], IsFilter = true });
        room.Box(0, 3, "wf_act_user_to_furni", Effect,
            new FurniWired { StuffIds = [mat.Item.Id], UserSources = [WiredSources.SelectedUsers], IntParams = [2] });
        var inside = room.User(5, 2, "inside");
        var outside = room.User(1, 0, "outside");
        var walker = room.User(2, 1);

        await room.StepAsync(walker, 3, 1);

        Assert.Equal((6, 3), (inside.X, inside.Y));
        Assert.Equal((1, 0), (outside.X, outside.Y));
        Assert.Equal((3, 1), (walker.X, walker.Y));
    }

    /// <summary>A stack a walk onto the plate at (3, 1) runs, with the given boxes above the trigger.</summary>
    private static (Room Room, RoomEntityStore.RoomEntity User) Stack(Action<Room> boxes)
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        boxes(room);
        return (room, room.User(2, 1));
    }

    [Fact]
    public async Task TheExecutionLimitDropsRunsBeyondItsCount()
    {
        RoomItemStore.PlacedItem lamp = null!;
        var (room, user) = Stack(r =>
        {
            lamp = r.Furni(6, 1);
            r.Box(0, 3, "wf_xtra_execution_limit", Addon, new FurniWired { IntParams = [1, 4] });
            r.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        });

        await room.StepAsync(user, 3, 1);
        await room.StepAsync(user, 2, 1);
        await room.StepAsync(user, 3, 1); // within the window: dropped
        Assert.Equal("1", lamp.Data.State);

        for (var pulse = 0; pulse < 4; pulse++)
            await room.Wired.PulseAsync();
        await room.StepAsync(user, 2, 1);
        await room.StepAsync(user, 3, 1);
        Assert.Equal("0", lamp.Data.State);
    }

    [Fact]
    public async Task ConditionEvaluationCanAskForAtLeastOne()
    {
        RoomItemStore.PlacedItem lamp = null!;
        var (room, user) = Stack(r =>
        {
            lamp = r.Furni(6, 1);
            r.Box(0, 3, "wf_xtra_or_eval", Addon, new FurniWired { IntParams = [1, 0, 0] });
            r.Box(0, 3, "wf_cnd_user_count_in", Condition, new FurniWired { IntParams = [5, 10] }); // fails
            r.Box(0, 3, "wf_cnd_wearing_effect", Condition, new FurniWired { IntParams = [0] }); // passes
            r.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        });

        await room.StepAsync(user, 3, 1);

        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task ConditionEvaluationCountsOnlyThePickedConditions()
    {
        RoomItemStore.PlacedItem lamp = null!;
        var (room, user) = Stack(r =>
        {
            lamp = r.Furni(6, 1);
            var failing = r.Box(0, 3, "wf_cnd_user_count_in", Condition, new FurniWired { IntParams = [5, 10] });
            r.Box(0, 3, "wf_cnd_not_wearing_fx", Condition, new FurniWired { IntParams = [0] }); // fails, not picked
            r.Box(0, 3, "wf_xtra_or_eval", Addon, new FurniWired { IntParams = [3, 0, 0], StuffIds = [failing.Item.Id] });
            r.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        });

        await room.StepAsync(user, 3, 1);

        // none of the picked pass, as asked, but the unpicked one must still pass, and does not
        Assert.Equal("0", lamp.Data.State);
    }

    [Fact]
    public async Task TheFilterCutsTheSelection()
    {
        var lamps = new List<RoomItemStore.PlacedItem>();
        var (room, user) = Stack(r =>
        {
            lamps.AddRange([r.Furni(5, 0, "lamp"), r.Furni(6, 0, "lamp"), r.Furni(7, 0, "lamp")]);
            r.Box(0, 3, "wf_slc_furni_area", Selector, new FurniWired { IntParams = [5, 0, 3, 1] });
            r.Box(0, 3, "wf_xtra_filter_furni", Addon, new FurniWired { IntParams = [2, WiredFilter.FixedAmount, 0] });
            r.Box(0, 3, "wf_act_toggle_state", Effect, new FurniWired { FurniSources = [WiredSources.SelectedFurni] });
        });

        await room.StepAsync(user, 3, 1);

        Assert.Equal(2, lamps.Count(lamp => lamp.Data.State == "1"));
    }

    [Fact]
    public async Task PlaceholdersFillTheStacksTexts()
    {
        var (room, user) = Stack(r =>
        {
            r.Box(0, 3, "wf_xtra_text_output_username", Addon, new FurniWired { StringParam = "who" });
            r.Box(0, 3, "wf_act_show_message", Effect, new FurniWired { StringParam = "Hello $(who)!" });
        });

        await room.StepAsync(user, 3, 1);

        Assert.Contains(room.Sent, sent => sent.Message is Habbo.Protocol.Messages.Outgoing.Room.Chat.WhisperMessageComposer { Text: "Hello tester!" });
    }

    [Fact]
    public void APlaceholderNameMustBeLettersNumbersAndUnderscores()
    {
        var addon = Boxes.For("wf_xtra_text_output_username")!;

        Assert.Null(addon.Validate(new FurniWired { StringParam = "user_name1" }));
        Assert.Equal("wiredfurni.error.placeholder.name_syntax", addon.Validate(new FurniWired { StringParam = "no-dash" })!.LocalizationKey);
        Assert.Equal("wiredfurni.error.placeholder.delimiter_empty",
            addon.Validate(new FurniWired { StringParam = "who", IntParams = [1] })!.LocalizationKey);
    }

    [Fact]
    public async Task MovementAddOnsShapeTheStacksMoves()
    {
        RoomItemStore.PlacedItem ball = null!;
        var (room, user) = Stack(r =>
        {
            ball = r.Furni(5, 0, "ball");
            r.Box(0, 3, "wf_xtra_anim_time", Addon, new FurniWired { IntParams = [1500] });
            r.Box(0, 3, "wf_xtra_mov_physics", Addon, new FurniWired { IntParams = [1, 0, 1, 0] });
            r.Box(0, 3, "wf_xtra_mov_carry_users", Addon, new FurniWired { IntParams = [CarryUsersAddon.OnItsTiles] });
            r.Box(0, 3, "wf_act_rel_mov", Effect, new FurniWired { StuffIds = [ball.Item.Id], IntParams = [1, 0] });
        });

        await room.StepAsync(user, 3, 1);

        var options = Assert.Single(room.MoveOptions);
        Assert.Equal(1500, options.AnimationMilliseconds);
        Assert.True(options.KeepAltitude);
        Assert.False(options.AvatarsBlock);
        Assert.Equal(CarryUsersAddon.OnItsTiles, options.CarryMode);

        var (room2, user2) = Stack(r =>
        {
            ball = r.Furni(5, 0, "ball");
            r.Box(0, 3, "wf_xtra_anim_time", Addon, new FurniWired { IntParams = [1500] });
            r.Box(0, 3, "wf_xtra_mov_no_animation", Addon, new FurniWired());
            r.Box(0, 3, "wf_act_rel_mov", Effect, new FurniWired { StuffIds = [ball.Item.Id], IntParams = [1, 0] });
        });
        await room2.StepAsync(user2, 3, 1);
        Assert.Equal(0, Assert.Single(room2.MoveOptions).AnimationMilliseconds);
    }

    [Fact]
    public async Task TheRandomAddOnAvoidsRecentPicks()
    {
        var lamps = new List<RoomItemStore.PlacedItem>();
        var (room, user) = Stack(r =>
        {
            r.Box(0, 3, "wf_xtra_random", Addon, new FurniWired { IntParams = [1, 1] });
            foreach (var x in new[] { 5, 6 })
            {
                var lamp = r.Furni(x, 0);
                lamps.Add(lamp);
                r.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
            }
        });

        // two runs, each avoiding the last one's pick: both lamps switch once
        await room.StepAsync(user, 3, 1);
        await room.StepAsync(user, 2, 1);
        await room.StepAsync(user, 3, 1);

        Assert.All(lamps, lamp => Assert.Equal("1", lamp.Data.State));
    }

    [Fact]
    public async Task TheRemoteSelectorUsesAnotherStacksSelection()
    {
        RoomItemStore.PlacedItem near = null!, far = null!;
        var (room, user) = Stack(r =>
        {
            near = r.Furni(5, 0, "near");
            far = r.Furni(7, 3, "far");
            // a stack elsewhere that only selects: the furni in its area
            var remote = r.Box(7, 0, "wf_slc_furni_area", Selector, new FurniWired { IntParams = [4, 0, 3, 1] });
            r.Box(0, 3, "wf_slc_remote", Selector, new FurniWired { StuffIds = [remote.Item.Id], IntParams = [0, 0] });
            r.Box(0, 3, "wf_act_toggle_state", Effect, new FurniWired { FurniSources = [WiredSources.SelectedFurni] });
        });

        await room.StepAsync(user, 3, 1);

        Assert.Equal("1", near.Data.State);
        Assert.Equal("0", far.Data.State);
    }

    [Fact]
    public async Task TheLogEffectsWriteToTheRoomsLog()
    {
        var (room, user) = Stack(r =>
        {
            r.Box(0, 3, "wf_xtra_text_output_username", Addon, new FurniWired { StringParam = "who" });
            r.Box(0, 3, "wf_act_log", Effect, new FurniWired { IntParams = [WiredLogLevels.Warn], StringParam = "$(who) stepped on it" });
            r.Box(0, 3, "wf_cnd_user_count_in", Condition, new FurniWired { IntParams = [5, 10] }); // fails
            r.Box(0, 3, "wf_act_neg_log", Effect, new FurniWired { IntParams = [WiredLogLevels.Debug], StringParam = "too few" });
        });

        await room.StepAsync(user, 3, 1);

        // the conditions failed: only the negative box wrote
        Assert.Equal([(WiredLogLevels.Debug, WiredLogLevels.WiredSource, "too few")], room.Logs);
    }

    [Fact]
    public async Task ARunawayStackIsCountedInTheMonitor()
    {
        var (room, user) = Stack(r => { });
        var plate = room.Items.Items.First(i => i.Item.Definition!.ClassName == "plate");
        // a stack that sets itself off: its effect toggles the lamp, which fires its trigger
        var lamp = room.Furni(6, 1);
        room.Box(1, 3, "wf_trg_stuff_state", Trigger, new FurniWired { StuffIds = [lamp.Item.Id], IntParams = [0] });
        room.Box(1, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));

        await room.StepAsync(user, plate.X, plate.Y);

        var error = Assert.Single(room.Wired.Errors);
        Assert.Equal(WiredErrors.ExecutionLimit, error.Kind);
        Assert.Contains(room.Logs, log => log.Source == WiredLogLevels.SystemSource && log.Level == WiredLogLevels.Warn);
    }

    [Fact]
    public async Task AMessageUsesTheStyleItsDialogChose()
    {
        var (room, user) = Stack(r =>
            r.Box(0, 3, "wf_act_show_message", Effect, new FurniWired { StringParam = "careful", IntParams = [0, 212] }));

        await room.StepAsync(user, 3, 1);

        Assert.Contains(room.Sent, sent => sent.Message is Habbo.Protocol.Messages.Outgoing.Room.Chat.WhisperMessageComposer { StyleId: 212 });
    }

    [Fact]
    public async Task ATimeConditionWithoutAZoneUsesTheRooms()
    {
        var now = new DateTimeOffset(2026, 3, 18, 14, 30, 15, TimeSpan.Zero);
        var (room, lamp, user) = Guarded("wf_cnd_match_time",
            new FurniWired { IntParams = [0, 0, 1, 0, 0, 0, 0, 15, 15] });
        room.Wired.Clock = () => now;
        room.Wired.Timezone = "Europe/Copenhagen";

        await room.StepAsync(user, 3, 1);

        Assert.Equal("1", lamp.Data.State);
    }

    /// <summary>A variable box on (7, 3), made as the dialog would save it.</summary>
    private static string Variable(Room room, string className, FurniWired settings) =>
        room.Box(7, 3, className, "wired_variable", settings).Item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>What saving a variable box's dialog answers.</summary>
    private static async Task<IComposer?> SaveVariable(Room room, RoomItemStore.PlacedItem box, string name, List<int> intParams) =>
        await room.Wired.SaveAsync(new WiredSaveData(WiredKind.Variable, box.Item.Id, intParams, name, [], [], [], [], []), _ => true);

    [Fact]
    public void EveryDialogsParametersFitWhatASaveMayCarry()
    {
        // a save past the limit is dropped; Habbo's neighbourhood dialog sends 18, its levelling add-on 22
        var registry = new WiredBoxRegistry();
        Assert.All(registry.ClassNames, name => Assert.InRange(registry.For(name)!.DefaultIntParams.Count, 0, WiredBoxType.MaxIntParams));
        Assert.Equal(18, registry.For("wf_slc_furni_neighborhood")!.DefaultIntParams.Count);
        Assert.Equal(22, WiredBoxType.MaxIntParams);
    }

    [Fact]
    public async Task AVariableNameIsTheRoomsAloneWhateverItBelongsTo()
    {
        // Habbo (p44 run 62): a context variable may not take a furni variable's name
        var room = new Room();
        Variable(room, "wf_var_furni", new FurniWired { StringParam = "points", IntParams = [1, 1] });
        var context = room.Box(6, 3, "wf_var_context", "wired_variable", new FurniWired { StringParam = "other", IntParams = [1] });

        var answer = await SaveVariable(room, context, "Points", [1]);

        Assert.Equal("wiredfurni.error.variables.name_uniq", Assert.IsType<WiredValidationErrorComposer>(answer).LocalizationKey);
    }

    [Fact]
    public async Task ARoomKeepsAtMostTwentyTwoVariablesPermanently()
    {
        // Habbo (p44 run 61) refused the 23rd permanent definition
        var room = new Room();
        for (var i = 0; i < 22; i++)
            Variable(room, "wf_var_furni", new FurniWired { StringParam = $"kept{i}", IntParams = [1, WiredVariableAvailability.Permanent] });
        var last = room.Box(6, 3, "wf_var_furni", "wired_variable", new FurniWired { StringParam = "last", IntParams = [1, 1] });

        var answer = await SaveVariable(room, last, "last", [1, WiredVariableAvailability.Permanent]);

        Assert.Equal("wiredfurni.error.variables.permanent_limit", Assert.IsType<WiredValidationErrorComposer>(answer).LocalizationKey);
    }

    /// <summary>A display of the class given, stacked with a user variable "hp" at (7, 3).</summary>
    private static (Room Room, WiredBox Display, WiredVariable Variable) Displaying(string className, FurniWired? settings = null)
    {
        var room = new Room();
        var hp = Variable(room, "wf_var_user", new FurniWired { StringParam = "hp", IntParams = [0, 1] });
        var item = room.Box(7, 3, className, Addon, settings ?? new FurniWired());
        return (room, room.Wired.StackAt(7, 3)!.Single(box => box.Item == item), room.Wired.FindVariable(hp)!);
    }

    private static Habbo.Silo.Wired.Addons.VariableFx.VariableFxAddon Fx(WiredBox box) => (Habbo.Silo.Wired.Addons.VariableFx.VariableFxAddon)box.Type;

    [Fact]
    public void ANewHealthDisplaySendsTheConfigHabbosDid()
    {
        // Habbo (experiment.fx, [14]): over users, always, mask 7, 3000 ms, health style 0, colour 1000, width 2,
        // renderer 10, 0 to 100, icon misc_heart
        var (_, display, _) = Displaying("wf_xtra_varfx_hp");

        var config = Fx(display).Config(display);

        Assert.Equal((display.Id, true, 0, 7, false, 3000), (config.ConfigId, config.IsUserFx, config.ShowMode, config.UpdateMask, config.ShowOnMouseHover, config.ShowDuration));
        Assert.Equal((0, 0, 1000, 2, 10, 0L, 100L), (config.CategoryId, config.StyleId, config.ColorId, config.WidthId, config.RendererId, config.DefaultMin, config.DefaultMax));
        Assert.Equal([new("icon", "misc_heart")], config.Extras);
    }

    [Theory]
    [InlineData("wf_xtra_varfx_boss", 4, 5, 4, 100)]
    [InlineData("wf_xtra_varfx_status", 3, -1, 2, 2)]
    public void ADisplaysStyleFillsInWhatItLeavesOpen(string className, int category, int color, int width, int renderer)
    {
        // Habbo ([291], [135]): the boss bar red, extra large, renderer 100; the energy status bar block, medium
        var (_, display, _) = Displaying(className);

        var config = Fx(display).Config(display);

        Assert.Equal((category, color, width, renderer), (config.CategoryId, config.ColorId, config.WidthId, config.RendererId));
    }

    [Fact]
    public async Task AChangedValueGoesToTheDisplaysAudience()
    {
        // Habbo ([19]): one status, not initial, keyed "display|variable", the user's room index, the value
        var (room, display, variable) = Displaying("wf_xtra_varfx_hp");
        var user = room.User(2, 1);

        await room.Wired.GiveVariableAsync(variable, WiredHolder.Of(user), null, 11, false);

        var status = Assert.Single(room.Sent.Select(sent => sent.Message)
            .OfType<Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.VariableFx.VariableFxStatusMessageComposer>());
        var entry = Assert.Single(status.Statuses);
        Assert.False(status.AllInitialize);
        Assert.Equal(($"{display.Id}|{variable.Id}", false, true, user.RoomIndex, 11L), (entry.StatusKey, entry.Initialize, entry.IsUserEntity, entry.EntityId, entry.Value));
    }

    [Fact]
    public void ALevellingDisplayShowsTheLevelItsSystemGives()
    {
        // Habbo ([85]): 19 experience at 10 a level is level 2 of 5, between 10 and 20
        var (room, display, variable) = Displaying("wf_xtra_varfx_levelling");
        room.Box(7, 3, "wf_xtra_var_lvlup_system", Addon, new FurniWired { IntParams = [0, 1, 10, 5, 50] });
        var user = room.User(2, 1);

        var status = Fx(display).Status(display, variable, WiredHolder.Of(user), 19, initialize: true, room.Wired);

        Assert.Equal(new(10, 20), status.Override);
        Assert.Equal([new("max_level", "5"), new("current_level", "2"), new("is_maxed", "false")], status.Extras);
        Assert.Equal((-1L, -1L), (Fx(display).Config(display).DefaultMin, Fx(display).Config(display).DefaultMax));
    }

    [Fact]
    public async Task UsersAreGivenVariablesAndTheirValuesChange()
    {
        var room = new Room();
        var points = Variable(room, "wf_var_user", new FurniWired { StringParam = "points", IntParams = [0, 1] });
        var plate = room.Furni(3, 1, "plate");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_xtra_exec_in_order", Addon, new FurniWired());
        room.Box(0, 3, "wf_act_give_var", Effect, new FurniWired { VariableIds = [points], IntParams = [1, 0, 10, 0] });
        room.Box(0, 3, "wf_act_change_var_val", Effect,
            new FurniWired { VariableIds = [points, ""], IntParams = [1, 1, 0, 0, 5, 1] });
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);
        var variable = room.Wired.FindVariable(points)!;
        Assert.Equal(15, room.Wired.VariableValue(variable, Habbo.Silo.Wired.Variables.WiredHolder.Of(user), null));

        // given again without overriding: kept, then 5 more
        await room.StepAsync(user, 2, 1);
        await room.StepAsync(user, 3, 1);
        Assert.Equal(20, room.Wired.VariableValue(variable, Habbo.Silo.Wired.Variables.WiredHolder.Of(user), null));
    }

    [Fact]
    public async Task GivingOverAHeldVariableGivesItAnew()
    {
        var room = new Room();
        var points = Variable(room, "wf_var_user", new FurniWired { StringParam = "points", IntParams = [0, 1] });
        var lamp = room.Furni(6, 1);
        // fires on "given" only
        room.Box(0, 3, "wf_trg_var_changed", Trigger, new FurniWired { VariableIds = [points], IntParams = [1, 0, 0, 7] });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);
        var variable = room.Wired.FindVariable(points)!;
        var holder = Habbo.Silo.Wired.Variables.WiredHolder.Of(user);
        var start = new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        room.Wired.Clock = () => start;

        await room.Wired.GiveVariableAsync(variable, holder, null, 5, false);
        Assert.Equal("1", lamp.Data.State);

        // without overriding: nothing happens
        Assert.False(await room.Wired.GiveVariableAsync(variable, holder, null, 7, false));
        Assert.Equal(5, room.Wired.VariableValue(variable, holder, null));
        Assert.Equal("1", lamp.Data.State);

        // overriding: the new value, created now, and "given" fires again (Habbo: give_override_event_kind)
        room.Wired.Clock = () => start.AddMinutes(1);
        Assert.True(await room.Wired.GiveVariableAsync(variable, holder, null, 9, true));
        Assert.Equal(9, room.Wired.VariableValue(variable, holder, null));
        Assert.Equal(start.AddMinutes(1), room.Wired.VariableTimes(variable, holder, null)!.Created);
        Assert.Equal("0", lamp.Data.State);
    }

    [Theory]
    [InlineData("wf_act_toggle_state", true)]
    [InlineData("wf_act_give_var", true)]
    [InlineData("wf_cnd_has_var", true)]
    [InlineData("wf_cnd_neg_has_var", true)]
    [InlineData("wf_trg_stuff_state", true)]
    [InlineData("wf_slc_furni_picks", true)]
    [InlineData("wf_act_chase", false)]
    [InlineData("wf_trg_walks_on_furni", false)]
    [InlineData("wf_cnd_furnis_hv_avtrs", false)]
    public void WallFurniMayBePickedWhereHabbosDialogsAllowIt(string className, bool allowed) =>
        Assert.Equal(allowed, new WiredBoxRegistry().For(className)!.AllowsWallFurni);

    [Fact]
    public async Task VariableConditionsReadHoldersAndValues()
    {
        var room = new Room();
        var level = Variable(room, "wf_var_user", new FurniWired { StringParam = "level", IntParams = [0, 1] });
        var lamp = room.Furni(6, 1);
        var plate = room.Furni(3, 1, "plate");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_cnd_has_var", Condition, new FurniWired { VariableIds = [level], IntParams = [1] });
        room.Box(0, 3, "wf_cnd_var_val_match", Condition,
            new FurniWired { VariableIds = [level, ""], IntParams = [1, WiredComparison.HigherOrEqual, 0, 0, 3, 1] });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);
        var variable = room.Wired.FindVariable(level)!;

        await room.StepAsync(user, 3, 1);
        Assert.Equal("0", lamp.Data.State); // not given yet

        await room.Wired.GiveVariableAsync(variable, Habbo.Silo.Wired.Variables.WiredHolder.Of(user), null, 2, false);
        await room.StepAsync(user, 2, 1);
        await room.StepAsync(user, 3, 1);
        Assert.Equal("0", lamp.Data.State); // 2 is below 3

        await room.Wired.SetVariableAsync(variable, Habbo.Silo.Wired.Variables.WiredHolder.Of(user), null, 4);
        await room.StepAsync(user, 2, 1);
        await room.StepAsync(user, 3, 1);
        Assert.Equal("1", lamp.Data.State);
    }

    [Theory]
    [InlineData(WiredVariableOrigins.ThisRoom, false, true)]
    [InlineData(WiredVariableOrigins.Inspection, true, false)]
    [InlineData(WiredVariableOrigins.ThisRoom | WiredVariableOrigins.Inspection, true, true)]
    [InlineData(WiredVariableOrigins.Every, true, true)]
    public async Task AVariableTriggerHearsOnlyTheOriginsItAllows(int origins, bool hearsInspection, bool hearsWired)
    {
        // Habbo (variable_event_origins): allowing only this room ignored an inspection give and heard a wired one;
        // allowing the inspection heard its create, set and delete
        var room = new Room();
        var score = Variable(room, "wf_var_room", new FurniWired { StringParam = "score", IntParams = [1] });
        var lamp = room.Furni(6, 1);
        room.Box(0, 3, "wf_trg_var_changed", Trigger,
            new FurniWired { VariableIds = [score], IntParams = [0, 1, 0, 7, origins] });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var variable = room.Wired.FindVariable(score)!;

        await room.Wired.EditVariableAsync(variable, WiredHolder.Room, 5, WiredVariableMenuActions.Set);
        Assert.Equal(hearsInspection ? "1" : "0", lamp.Data.State);

        lamp.Data = FurniDataView.WithState("0");
        await room.Wired.SetVariableAsync(variable, WiredHolder.Room, null, 6);
        Assert.Equal(hearsWired ? "1" : "0", lamp.Data.State);
    }

    [Fact]
    public async Task AChangedVariableFiresItsTrigger()
    {
        var room = new Room();
        var score = Variable(room, "wf_var_room", new FurniWired { StringParam = "score", IntParams = [1] });
        var lamp = room.Furni(6, 1);
        room.Box(0, 3, "wf_trg_var_changed", Trigger, new FurniWired { VariableIds = [score], IntParams = [0, 1, 0, 1] });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var variable = room.Wired.FindVariable(score)!;

        await room.Wired.SetVariableAsync(variable, Habbo.Silo.Wired.Variables.WiredHolder.Room, null, 5);
        Assert.Equal("1", lamp.Data.State);

        // decreasing is not what it listens for
        await room.Wired.SetVariableAsync(variable, Habbo.Silo.Wired.Variables.WiredHolder.Room, null, 1);
        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task InternalVariablesReadAndWriteTheRoom()
    {
        var room = new Room();
        var ball = room.Furni(2, 2, "ball");
        var position = room.Wired.FindVariable("@internal.0.@position_x")!;

        Assert.Equal(2, room.Wired.VariableValue(position, Habbo.Silo.Wired.Variables.WiredHolder.Of(ball), null));
        Assert.True(await room.Wired.SetVariableAsync(position, Habbo.Silo.Wired.Variables.WiredHolder.Of(ball), null, 5));
        Assert.Equal(5, ball.X);
    }

    [Fact]
    public void TheVariableListSyncsByHash()
    {
        var room = new Room();
        Variable(room, "wf_var_furni", new FurniWired { StringParam = "hp", IntParams = [1, 1] });

        var all = room.Wired.VariableDiffs(new Dictionary<string, int>());
        var added = all.SelectMany(chunk => chunk.AddedOrUpdated).ToList();
        Assert.True(all[^1].IsLastChunk);
        Assert.Contains(added, v => v.Variable.Name == "hp");

        var known = added.ToDictionary(v => v.Variable.Id, v => v.Hash);
        known["gone"] = 1;
        var diff = Assert.Single(room.Wired.VariableDiffs(known));
        Assert.Empty(diff.AddedOrUpdated);
        Assert.Equal(["gone"], diff.RemovedVariableIds);
        Assert.Equal(room.Wired.VariablesHash, diff.AllVariablesHash);
    }

    [Theory]
    [InlineData(WiredVariableUse.Add, 3, 4, 7)]
    [InlineData(WiredVariableUse.Divide, 7, 2, 3)]
    [InlineData(WiredVariableUse.Power, 2, 10, 1024)]
    [InlineData(WiredVariableUse.SetMinimum, 3, 5, 5)]
    [InlineData(WiredVariableUse.SetMaximum, 9, 5, 5)]
    [InlineData(WiredVariableUse.BitwiseXor, 6, 3, 5)]
    [InlineData(WiredVariableUse.ShiftLeft, 1, 4, 16)]
    [InlineData(WiredVariableUse.BitCount, 7, 0, 3)] // of the value: the dialog sends no operand for it
    [InlineData(WiredVariableUse.Multiply, int.MaxValue, 2, int.MaxValue)]
    public void VariableOperationsCompute(int operation, int value, int operand, int expected) =>
        Assert.Equal(expected, WiredVariableUse.Apply(operation, value, operand, Random.Shared));

    [Fact]
    public void DividingByZeroChangesNothing() =>
        Assert.Null(WiredVariableUse.Apply(WiredVariableUse.Divide, 5, 0, Random.Shared));

    [Fact]
    public async Task AnEchoReadsAndWritesWhatItEchoes()
    {
        var room = new Room();
        var ball = room.Furni(2, 2, "ball");
        var echo = Variable(room, "wf_var_echo", new FurniWired { StringParam = "x", VariableIds = ["@internal.0.@position_x"] });
        var variable = room.Wired.FindVariable(echo)!;

        Assert.Equal(WiredVariableTargets.Furni, variable.Target);
        Assert.Equal(2, room.Wired.VariableValue(variable, WiredHolder.Of(ball), null));
        await room.Wired.SetVariableAsync(variable, WiredHolder.Of(ball), null, 4);
        Assert.Equal(4, ball.X);
    }

    [Theory]
    [InlineData("wf_var_echo")]
    [InlineData("wf_var_reference")]
    public void AVariableLeadingBackToItselfIsPlainNotEndless(string className)
    {
        var room = new Room();
        var self = new FurniWired { StringParam = "self" };
        var selfId = Variable(room, className, self);
        self.VariableIds.Add(selfId);
        var first = new FurniWired { StringParam = "a" };
        var second = new FurniWired { StringParam = "b" };
        var firstId = Variable(room, className, first);
        var secondId = Variable(room, className, second);
        first.VariableIds.Add(secondId);
        second.VariableIds.Add(firstId);

        Assert.Null(room.Wired.FindVariable(selfId)!.EchoOf);
        Assert.NotNull(room.Wired.FindVariable(firstId));
        Assert.NotEmpty(room.Wired.Variables);
        _ = room.Wired.VariablesHash;
    }

    [Theory]
    [InlineData("#(a) and #(A)", "4 and 4", true)]
    [InlineData("#(a) and #(A)", "4 and 5", false)]
    [InlineData("#(a)#(b)#(c)#(d)#(e)#(f)", "a b c d e f", false)]
    [InlineData("#(1a) here", "#(1a) here", true)]
    public async Task KeywordCapturesMatchAsTheyShould(string keyword, string said, bool heard)
    {
        var room = new Room();
        var number = Variable(room, "wf_var_user", new FurniWired { StringParam = "number", IntParams = [0, 1] });
        room.Box(0, 3, "wf_trg_says_something", Trigger, new FurniWired { StringParam = keyword, IntParams = [0, 0, 0] });
        room.Box(0, 3, "wf_xtra_text_input_variable", Addon, new FurniWired { StringParam = "a", VariableIds = [number], IntParams = [0] });
        room.Box(0, 3, "wf_act_show_message", Effect, new FurniWired { StringParam = "heard" });
        var user = room.User(2, 1);

        await room.Wired.UserSaidAsync(user, said);

        Assert.Equal(heard, Heard(room));
    }

    private static bool Heard(Room room) =>
        room.Sent.Any(sent => sent.Message is Habbo.Protocol.Messages.Outgoing.Room.Chat.WhisperMessageComposer { Text: "heard" });

    /// <summary>A stack hearing <paramref name="keyword"/> said exactly, with a capturer for each of
    /// <paramref name="capturers"/> into a user variable whose text connector is <paramref name="connector"/> (text
    /// mode), or into a number when there is no connector.</summary>
    private static (Room Room, RoomEntityStore.RoomEntity User, Func<string, int?> Value) Capturing(string keyword,
        string[] capturers, string? connector = null)
    {
        var room = new Room();
        var variables = capturers.ToDictionary(name => name,
            name => Variable(room, "wf_var_user", new FurniWired { StringParam = name, IntParams = [0, 1] }));
        if (connector is not null)
            room.Box(7, 3, "wf_xtra_var_text_connector", Addon, new FurniWired { StringParam = connector });
        room.Box(0, 3, "wf_trg_says_something", Trigger, new FurniWired { StringParam = keyword, IntParams = [0, 1, 0] });
        foreach (var name in capturers)
        {
            room.Box(0, 3, "wf_xtra_text_input_variable", Addon,
                new FurniWired { StringParam = name, VariableIds = [variables[name]], IntParams = [connector is null ? 0 : 1] });
        }
        room.Box(0, 3, "wf_act_show_message", Effect, new FurniWired { StringParam = "heard" });
        var user = room.User(2, 1);
        return (room, user, name => room.Wired.VariableValue(room.Wired.FindVariable(variables[name])!, WiredHolder.Of(user), null));
    }

    [Fact]
    public async Task ACaptureWithoutACapturerStopsTheTrigger()
    {
        // Habbo (p33_chat): every marker needs its own capturer
        var (room, user, _) = Capturing("go #(x) #(y)", ["x"]);

        await room.Wired.UserSaidAsync(user, "go 3 4");

        Assert.False(Heard(room));
    }

    [Fact]
    public async Task ATextNoConnectorMapsStopsTheTrigger()
    {
        // Habbo (p33_chat): a character missing from the mapping stops the trigger
        var (room, user, value) = Capturing("qr:#(a)#(b)", ["a", "b"], "0=x\n1=y");

        await room.Wired.UserSaidAsync(user, "qr:yz");
        Assert.False(Heard(room));

        await room.Wired.UserSaidAsync(user, "qr:yx");
        Assert.True(Heard(room));
        Assert.Equal(1, value("a"));
        Assert.Equal(0, value("b"));
    }

    [Fact]
    public async Task AdjacentCapturesTakeOneCharacterAndTrailingOnesMayBeEmpty()
    {
        // Habbo (p25_length): one character per adjacent marker, trailing ones may be empty when the mapping has
        // "0=", and a longer text never fires
        var (room, user, value) = Capturing("qr:#(a)#(b)#(c)", ["a", "b", "c"], "0=\n1=x\n2=y");

        await room.Wired.UserSaidAsync(user, "qr:xyxy");
        Assert.False(Heard(room));

        await room.Wired.UserSaidAsync(user, "qr:y");
        Assert.True(Heard(room));
        Assert.Equal((2, 0, 0), (value("a"), value("b"), value("c")));
    }

    [Fact]
    public async Task CapturesReadTheMessageLowercasedAndTheConnectorExactFirst()
    {
        // Habbo (p47_case): the chat text is lowercased before capturing; the connector matches exact, then any case
        var (room, user, value) = Capturing("set #(a)", ["a"], "1=A\n2=a\n3=Big");

        await room.Wired.UserSaidAsync(user, "set A");
        Assert.Equal(2, value("a"));

        await room.Wired.UserSaidAsync(user, "set BIG");
        Assert.Equal(3, value("a"));
    }

    [Theory]
    [InlineData(191, true)]
    [InlineData(202, false)]
    public async Task AKeywordWithCapturesIsHeardUpToAboutTwoHundredCharacters(int length, bool heard)
    {
        // Habbo (p41_length): 191 characters fired, 202 did not
        var prefix = new string('q', length - "#(a)".Length);
        var (room, user, _) = Capturing(prefix + "#(a)", ["a"]);

        await room.Wired.UserSaidAsync(user, prefix + "5");

        Assert.Equal(heard, Heard(room));
    }

    [Fact]
    public void TwoCapturersOfOneNameMayNotShareAStack()
    {
        var (room, _, _) = Capturing("go #(x)", ["x"]);
        var stack = room.Wired.StackAt(0, 3)!;
        var type = new WiredBoxRegistry().For("wf_xtra_text_input_variable")!;

        var refusal = type.ValidateInStack(new FurniWired { StringParam = "X" }, stack[0], stack);

        Assert.Equal("wiredfurni.error.capturer.duplicate", refusal?.LocalizationKey);
    }

    [Fact]
    public void SaysSomethingReadsItsSettingsInTheClientsOrder()
    {
        // the client sends [only owner, match mode, hide message]; mode 2, "match all text", is any message at all
        var room = new Room();
        room.Box(1, 1, "wf_trg_says_something", Trigger, new FurniWired { StringParam = "", IntParams = [0, 2, 1] });
        var user = room.User(2, 1);

        Assert.True(room.Wired.Hides(user, "whatever is said"));
    }

    [Theory]
    [InlineData(0, true)]  // any of them has an avatar on it
    [InlineData(1, false)] // all of them must
    public async Task FurnisHaveAvatarsCountsItsRadioAsZeroAnyOneAll(int radio, bool passes)
    {
        var room = new Room();
        var lamp = room.Furni(6, 1);
        var plate = room.Furni(3, 1, "plate");
        var empty = room.Furni(5, 3, "plate");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_cnd_furnis_hv_avtrs", Condition, new FurniWired
        {
            StuffIds = [plate.Item.Id, empty.Item.Id], IntParams = [radio],
        });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal(passes ? "1" : "0", lamp.Data.State);
    }

    [Theory]
    [InlineData(Habbo.Silo.Wired.Conditions.WiredQuantity.All, true)]
    [InlineData(Habbo.Silo.Wired.Conditions.WiredQuantity.Any, false)]
    public async Task HasVariableOverNoHoldersPassesAllAndFailsAny(int quantifier, bool passes)
    {
        // Habbo, measured: an empty source passes the "all" quantifier and fails "any"
        var room = new Room();
        var level = Variable(room, "wf_var_furni", new FurniWired { StringParam = "level", IntParams = [0, 1] });
        var lamp = room.Furni(6, 1);
        var plate = room.Furni(3, 1, "plate");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        // a furni variable over picked furni, with nothing picked
        room.Box(0, 3, "wf_cnd_has_var", Condition, new FurniWired { VariableIds = [level], IntParams = [0], Quantifier = quantifier });
        room.Box(0, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);

        Assert.Equal(passes ? "1" : "0", lamp.Data.State);
    }

    [Fact]
    public async Task AKeywordThatWouldBacktrackForEverGivesUpInsteadOfHangingTheRoom()
    {
        var room = new Room();
        room.Box(0, 3, "wf_trg_says_something", Trigger, new FurniWired { StringParam = "#(a)#(b)#(c)#(d)#(e)!", IntParams = [0, 0, 0] });
        var user = room.User(2, 1);
        var watch = System.Diagnostics.Stopwatch.StartNew();

        await room.Wired.UserSaidAsync(user, new string('x', 4000));

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ACaptureStoresWhatWasSaidAndATextConnectorNamesIt()
    {
        var room = new Room();
        var mood = room.Box(7, 3, "wf_var_user", "wired_variable", new FurniWired { StringParam = "mood", IntParams = [0, 1] });
        room.Box(7, 3, "wf_xtra_var_text_connector", Addon, new FurniWired { StringParam = "1=happy\n2=sad" });
        var moodId = mood.Item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        room.Box(0, 3, "wf_trg_says_something", Trigger, new FurniWired { StringParam = "i feel #(m)", IntParams = [0, 1, 0] });
        room.Box(0, 3, "wf_xtra_text_input_variable", Addon, new FurniWired { StringParam = "m", IntParams = [1], VariableIds = [moodId] });
        room.Box(0, 3, "wf_xtra_text_output_variable", Addon,
            new FurniWired { StringParam = "now", IntParams = [0, WiredVariableTargets.User, 1], VariableIds = [moodId] });
        room.Box(0, 3, "wf_act_show_message", Effect, new FurniWired { StringParam = "You are $(now)" });
        var user = room.User(2, 1);

        await room.Wired.UserSaidAsync(user, "I feel sad");

        Assert.Equal(2, room.Wired.VariableValue(room.Wired.FindVariable(moodId)!, WiredHolder.Of(user), null));
        Assert.Contains(room.Sent, sent => sent.Message is Habbo.Protocol.Messages.Outgoing.Room.Chat.WhisperMessageComposer { Text: "You are sad" });
    }

    [Fact]
    public async Task ALevelSystemGivesItsVariableSubVariables()
    {
        var room = new Room();
        var xp = room.Box(7, 3, "wf_var_user", "wired_variable", new FurniWired { StringParam = "xp", IntParams = [0, 1] });
        // linear: 100 xp a level, 10 levels; every sub-variable
        room.Box(7, 3, "wf_xtra_var_lvlup_system", Addon, new FurniWired { IntParams = [0xFF, 1, 100, 10] });
        var id = xp.Item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var user = room.User(2, 1);
        await room.Wired.GiveVariableAsync(room.Wired.FindVariable(id)!, WiredHolder.Of(user), null, 250, false);

        int? Sub(string name) => room.Wired.VariableValue(room.Wired.FindVariable(id + "." + name)!, WiredHolder.Of(user), null);

        Assert.Contains(room.Wired.Variables, v => v.Name == "xp.current_level" && v.Kind == WiredVariableKinds.SubVariable);
        Assert.Equal(3, Sub("current_level"));
        Assert.Equal(50, Sub("progress"));
        Assert.Equal(50, Sub("progress_percentage"));
        Assert.Equal(100, Sub("xp_required"));
        Assert.Equal(50, Sub("xp_remaining"));
        Assert.Equal(0, Sub("is_maxed"));
        Assert.Equal(10, Sub("max_level"));
    }

    [Fact]
    public async Task ALevelSystemWithoutBoundsIsBoundedAndQuick()
    {
        var room = new Room();
        var xp = room.Box(7, 3, "wf_var_user", "wired_variable", new FurniWired { StringParam = "xp", IntParams = [0, 1] });
        // linear, no step, every level there is
        room.Box(7, 3, "wf_xtra_var_lvlup_system", Addon, new FurniWired { IntParams = [0xFF, 1, 0, int.MaxValue] });
        var id = xp.Item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var user = room.User(2, 1);
        await room.Wired.GiveVariableAsync(room.Wired.FindVariable(id)!, WiredHolder.Of(user), null, 250, false);
        var watch = System.Diagnostics.Stopwatch.StartNew();

        int? Sub(string name) => room.Wired.VariableValue(room.Wired.FindVariable(id + "." + name)!, WiredHolder.Of(user), null);

        // a step of nothing is a step of one
        Assert.Equal(1000, Sub("max_level"));
        Assert.Equal(251, Sub("current_level"));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ATimeUtilReadsTheVariablesValueAsATime()
    {
        var room = new Room();
        var when = room.Box(7, 3, "wf_var_room", "wired_variable", new FurniWired { StringParam = "when", IntParams = [1] });
        // hour of day, year, and days since 1970
        room.Box(7, 3, "wf_xtra_var_time_util", Addon, new FurniWired { IntParams = [(1 << 4) | (1 << 10) | (1 << 24), 0] });
        var id = when.Item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var moment = new DateTimeOffset(2026, 3, 18, 14, 30, 0, TimeSpan.Zero);
        await room.Wired.SetVariableAsync(room.Wired.FindVariable(id)!, WiredHolder.Room, null, (int)moment.ToUnixTimeSeconds());

        int? Sub(string name) => room.Wired.VariableValue(room.Wired.FindVariable(id + "." + name)!, WiredHolder.Room, null);

        Assert.Equal(14, Sub("hour_of_day"));
        Assert.Equal(2026, Sub("year"));
        Assert.Equal((int)(moment.ToUnixTimeSeconds() / 86400), Sub("day"));
    }

    [Theory]
    [InlineData(false, 4, 4, 3)]
    [InlineData(true, 235, 2, 1)]
    public async Task ATeleportSparklesMovesThenStopsSparkling(bool fast, int sparkle, int pulsesBeforeMove, int pulsesAfterMove)
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var target = room.Furni(7, 3, "target");
        room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(0, 3, "wf_act_teleport_to", Effect, new FurniWired { StuffIds = [target.Item.Id], IntParams = [fast ? 1 : 0] });
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);
        Assert.Equal(sparkle, user.EffectId); // at once, where they stand
        Assert.Equal((3, 1), (user.X, user.Y));

        for (var pulse = 1; pulse < pulsesBeforeMove; pulse++)
            await room.Wired.PulseAsync();
        Assert.Equal((3, 1), (user.X, user.Y));
        await room.Wired.PulseAsync();
        Assert.Equal((7, 3), (user.X, user.Y));

        for (var pulse = 1; pulse < pulsesAfterMove; pulse++)
            await room.Wired.PulseAsync();
        Assert.Equal(sparkle, user.EffectId);
        await room.Wired.PulseAsync();
        Assert.Equal(0, user.EffectId);
    }

    [Fact]
    public async Task ADelaySavedBeyondTheBoundWaitsOnlyAsLongAsTheBound()
    {
        // a row from before saves were bounded: waits the 20 pulses the dialog allows, not for ever
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var lamp = room.Furni(6, 1);
        room.Box(1, 1, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        var toggle = Picks(lamp);
        toggle.DelayInPulses = int.MaxValue;
        room.Box(1, 1, "wf_act_toggle_state", Effect, toggle);
        var user = room.User(2, 1);

        await room.StepAsync(user, 3, 1);
        for (var pulse = 1; pulse < 20; pulse++)
            await room.Wired.PulseAsync();
        Assert.Equal("0", lamp.Data.State);
        await room.Wired.PulseAsync();
        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task BotsHoldNoUserVariables()
    {
        // bots and pets all have the empty player id: a value one held would be every one's
        var room = new Room();
        var points = room.Wired.FindVariable(Variable(room, "wf_var_user", new FurniWired { StringParam = "points", IntParams = [0, 1] }))!;
        var bot = room.Bot(5, 1, "Frank");
        var user = room.User(2, 1);

        Assert.False(await room.Wired.GiveVariableAsync(points, WiredHolder.Of(bot), null, 7, overrideExisting: true));
        Assert.True(await room.Wired.GiveVariableAsync(points, WiredHolder.Of(user), null, 3, overrideExisting: true));

        Assert.Null(room.Wired.VariableValue(points, WiredHolder.Of(bot), null));
        Assert.Equal(3, room.Wired.VariableValue(points, WiredHolder.Of(user), null));
    }

    [Fact]
    public async Task ChestsPayUsersButNotBots()
    {
        var room = new Room();
        var rug = room.Furni(5, 1, "rug");
        var plate = room.Furni(3, 1, "plate");
        room.Box(0, 3, "wf_trg_bot_reached_stf", Trigger, new FurniWired { StringParam = "Frank", StuffIds = [rug.Item.Id] });
        room.Box(0, 3, "wf_act_give_currency", Effect, new FurniWired());
        room.Box(1, 3, "wf_trg_walks_on_furni", Trigger, Picks(plate));
        room.Box(1, 3, "wf_act_give_currency", Effect, new FurniWired());
        var bot = room.Bot(5, 1, "Frank");
        var user = room.User(2, 1);

        await room.Wired.BotArrivedAsync(bot);
        await room.StepAsync(user, 3, 1);

        Assert.Same(user, Assert.Single(room.Chests.Paid));
    }

    [Fact]
    public async Task AKeptChangeBeyondThePulsesWritesIsRefusedNotHeldUnkept()
    {
        var room = new Room();
        var rank = room.Wired.FindVariable(Variable(room, "wf_var_furni",
            new FurniWired { StringParam = "rank", IntParams = [1, WiredVariableAvailability.Permanent] }))!;
        var lamp = room.Furni(6, 1);
        room.Wired.RoomId = 1;

        // the pulse's writes spent (the same state again, so nothing else is set off)
        for (var write = 0; write < 2000; write++)
            await room.Wired.SetStateAsync(lamp, "0");

        Assert.False(await room.Wired.GiveVariableAsync(rank, WiredHolder.Of(lamp), null, 5, overrideExisting: true));
        Assert.Null(room.Wired.VariableValue(rank, WiredHolder.Of(lamp), null));
    }

    [Fact]
    public async Task UsingFurniOverAndOverSpendsTheVisitorsRunsNotTheRooms()
    {
        var room = new Room();
        var used = room.Furni(3, 1, "lever");
        var lamp = room.Furni(6, 1);
        room.Box(0, 3, "wf_trg_state_changed", Trigger, Picks(used));
        room.Box(1, 3, "wf_trg_period_short", Trigger, new FurniWired { IntParams = [1] });
        room.Box(1, 3, "wf_act_toggle_state", Effect, Picks(lamp));
        var user = room.User(2, 1);

        for (var use = 0; use <= 5000; use++)
            await room.Wired.FurniStateChangedAsync(user, used);
        await room.Wired.ShortTickAsync();

        Assert.Equal("1", lamp.Data.State);
    }

    [Fact]
    public async Task ATriggersStackSelectsOncePerEvent()
    {
        var room = new Room();
        var plate = room.Furni(3, 1, "plate");
        var trigger = room.Box(0, 3, "wf_trg_walks_on_furni", Trigger, new FurniWired { FurniSources = [WiredSources.SelectedFurni] });
        room.Box(0, 3, "wf_slc_furni_bytype", Selector, new FurniWired { StuffIds = [plate.Item.Id], FurniSources = [WiredSources.PickedFurni] });
        var box = room.Wired.StackAt(0, 3)!.Single(b => b.Item == trigger);
        var e = new WiredEvent(WiredEventType.Pulse);

        var first = room.Wired.SelectedFor(box, e);
        var again = room.Wired.SelectedFor(box, e);
        var another = room.Wired.SelectedFor(box, new WiredEvent(WiredEventType.Pulse));

        Assert.Same(first.Furni, again.Furni);
        Assert.NotSame(first.Furni, another.Furni);
        Assert.Equal([plate], another.Furni);
    }

    [Fact]
    public void TemporaryIdsAreUsedAgainOnceEveryFreshOneIs()
    {
        var room = new Room();
        var lamp = room.Furni(6, 1);
        var fresh = int.MaxValue - RoomItemStore.FirstTemporaryId;
        for (var copy = 0; copy < fresh; copy++)
            Assert.True(room.Items.Remove(room.Items.AddCopy(lamp, lamp.Data, 1, 1, null, 0)!.Item.Id));

        var reused = room.Items.AddCopy(lamp, lamp.Data, 1, 1, null, 0);
        Assert.Equal(RoomItemStore.FirstTemporaryId, reused!.Item.Id);
    }

    [Fact]
    public async Task AScoreAtItsTopStaysThere()
    {
        var room = new Room();

        await room.Games.AddScoreAsync(1, int.MaxValue);
        await room.Games.AddScoreAsync(1, 5);

        Assert.Equal(int.MaxValue, room.Games.Score(1));
    }
}
