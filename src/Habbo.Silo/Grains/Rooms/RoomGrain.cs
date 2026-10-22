using Habbo.Silo.Abstractions.Permissions;
using Habbo.Domain.Furniture;
using Habbo.Domain.Groups;
using Habbo.Domain.Rooms;
using Habbo.Infrastructure;
using Habbo.Protocol;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Handshake;
using Habbo.Protocol.Messages.Outgoing.Inventory.Furni;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.Room.Action;
using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.Room.Permissions;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Protocol.Messages.Outgoing.RoomSettings;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu.RoomLogs;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu.VariablesManagement;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Protocol.Rooms;
using Habbo.Silo.Abstractions.Achievements;
using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Abstractions.Wired;
using Habbo.Silo.Commands;
using Habbo.Silo.Items;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired;
using Habbo.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Habbo.Silo.Abstractions.Groups;
using Habbo.Silo.Achievements;
using Habbo.Silo.Permissions;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>One grain per room: loads the room, runs the 500ms tick, and broadcasts.
/// Avatar and furniture state live in their own stores.</summary>
public sealed partial class RoomGrain(
    IDbContextFactory<AppDbContext> dbFactory,
    IClientPacketSink sink,
    IGestureCatalog gestures,
    FurniInteractionRegistry interactions,
    FurniDataLoader furniData,
    WiredBoxRegistry wiredBoxes,
    IAvatarEffectCatalog avatarEffects,
    IGameTexts gameTexts,
    IClientVariables clientVariables,
    Microsoft.Extensions.Options.IOptions<RoomOptions> roomOptions,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Chat.ChatOptions> chatOptions,
    Habbo.Silo.Chat.WordFilter wordFilter,
    IPerkService perks,
    IGroupService groups,
    Microsoft.Extensions.Options.IOptions<TradingOptions> tradingOptions,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Items.FurnitureOptions> furnitureOptions,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Bots.BotOptions> botOptions,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Pets.PetOptions> petOptions,
    Habbo.Silo.Pets.PetRules petRules,
    Habbo.Silo.Pets.PetNames petNames,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Pets.PlantOptions> plantOptions,
    Habbo.Silo.Pets.PlantLooks plantLooks,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Items.RentableSpaceOptions> rentableOptions,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Items.MysteryTrophyOptions> mysteryTrophyOptions,
    IPollService polls,
    RoomSettingsRules settingsRules,
    Habbo.Silo.Chests.ChestRules chestRules,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Chests.ChestOptions> chestOptions,
    IDeferredNotificationService deferredNotifications,
    IEarningsService earnings,
    Habbo.Silo.Wired.Rewards.IWiredRewards wiredRewards,
    Habbo.Silo.Achievements.AchievementResolutions resolutions,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Navigator.NavigatorOptions> navigatorOptions,
    Habbo.Silo.Users.BadgeLeaderboards badgeLeaderboards,
    Habbo.Silo.Users.CurrencyService currencies,
    Microsoft.Extensions.Options.IOptions<Habbo.Silo.Users.CurrencyOptions> currencyOptions,
    PermissionService permissions,
    InvisiblePlayers invisiblePlayers,
    HotelCommandService hotelCommands,
    InventoryCommandService inventoryCommands,
    Habbo.Silo.Abstractions.Subscriptions.ISubscriptionService subscriptions,
    ILogger<RoomGrain> logger) : Grain, IGameRoomGrain
{
    /// <summary>The item types a pickup confirmation names, as the client's pickup sends them.</summary>
    private const int WallPickup = 1;
    private const int FloorPickup = 2;

    private const string VariablePickupTitle = "wiredfurni.variables.pickup.title";
    private const string VariablePickupBody = "wiredfurni.variables.pickup.body";

    /// <summary>The cadence the client animates one walking step over.</summary>
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(500);

    private IGrainTimer? _tickTimer;

    /// <summary>When the next tick is due.</summary>
    private DateTime _nextTickAt;

    /// <summary>The short repeat trigger's step: 50 milliseconds.</summary>
    private static readonly TimeSpan ShortTickInterval = TimeSpan.FromMilliseconds(50);

    /// <summary>How far ahead an occupied room keeps itself alive; refreshed on every tick.</summary>
    private static readonly TimeSpan OccupiedRoomLifetime = TimeSpan.FromMinutes(5);

    private Room? _room;
    private RoomModel? _model;
    private RoomItemStore? _items;
    private RoomEntityStore? _entities;
    private RoomFurnitureUse? _furniture;
    private RoomWired? _wired;
    private RoomGames? _games;

    /// <summary>The users with rights in the room.</summary>
    private readonly HashSet<Guid> _rights = [];

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var roomId = (int)this.GetPrimaryKeyLong();

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        _room = await db.Rooms
            .Include(r => r.Model)
            .Include(r => r.Owner)
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);

        if (_room?.Model is null)
            return;

        _model = RoomModelParser.Parse(_room.Model.Heightmap, _room.Model.DoorX, _room.Model.DoorY);
        _items = new RoomItemStore(_model, interactions);
        _entities = new RoomEntityStore(_model, _items) { AllowDiagonal = _room.AllowDiagonal };
        ApplyIdleTimes();
        _furniture = new RoomFurnitureUse(_items, dbFactory, GrainFactory, Broadcast, SendTo, Schedule,
            WalkThenAsync, LookAt, SetHandItem, ApplyEffect, DressInRoom, (x, y) => _entities!.HasAvatarAt(x, y));

        var placed = await WithDefinitions(db.UserItems)
            .Where(i => i.RoomId == roomId)
            .ToListAsync(cancellationToken);

        _items.Load(placed, await furniData.LoadAsync(db, placed, cancellationToken));
        _rights.UnionWith(await db.RoomRights.Where(r => r.RoomId == roomId).Select(r => r.UserId).ToListAsync(cancellationToken));
        await LoadGroupAsync(db);
        await LoadRoomRulesAsync(db);
        await LoadEventAsync(db);
        await LoadStackHeightsAsync(db);
        _items.RefreshWaterAreas();
        await LoadBotsAsync(db);
        await LoadPetsAsync(db);
        await LoadRentalsAsync(db);
        await LoadChestHoldsAsync(db);
        await SyncChestCountsAsync();
        RememberLoadedFurni();

        _games = new RoomGames(_items, _entities, _furniture, avatarEffects, ApplyEffect, new GameRoomActions(
            (item, x, y, rotation, intoFurni) => MovePlacedFloorItemAsync(item, x, y, rotation, wired: null, intoFurni, anyDirection: true),
            _entities.HasAvatarAt,
            (entity, x, y) => Broadcast(UserUpdateMessageComposer.From([_entities.Warp(entity, x, y)])),
            SetGameFrozen,
            (entity, achievement, amount) =>
            {
                if (entity.Bot is null && entity.Pet is null)
                    GrainFactory.GetGrain<IPlayerGrain>(entity.Player.Id).ProgressAchievementAsync(achievement, amount).Ignore();
            },
            (achievement, amount) => GrainFactory.GetGrain<IPlayerGrain>(_room.OwnerId).ProgressAchievementAsync(achievement, amount).Ignore()));
        _wired = new RoomWired(_room.OwnerId, _items, _entities, _games, wiredBoxes, _furniture, dbFactory,
            Broadcast, SendTo,
            new WiredRoomActions((item, x, y, rotation, options) => MovePlacedFloorItemAsync(item, x, y, rotation, options), SetPlacedAltitudeAsync, ApplyEffect, KickAsync, gameTexts, WriteWiredLogAsync,
                (entity, style) => DanceAsync(entity.Player.Id, style),
                (entity, sign) => ShowSignAsync(entity.Player.Id, sign),
                change => GrainFactory.GetGrain<IGameRoomGrain>(change.RoomId).SharedVariableChangedAsync(change),
                avatarEffects, new WiredChestAccess(this), wiredRewards)
            {
                Bots = new WiredBotActions(WiredBotSayAsync, DressBotAsync, HandOverAsync, WalkUpToAsync),
                SendThrough = SendThroughTeleportAsync,
            },
            logger)
        {
            RoomId = _room.Id,
            GroupId = _group?.Id,
        };
        _wired.Timezone = _games.Timezone = await WiredTimezoneAsync();
        _games.HighScoreMaxEntries = roomOptions.Value.HighScoreMaxEntries;
        await _games.ClearStaleHighScoresAsync();
        _games.OnEvent = _wired.GameEventAsync;
        _furniture.OnStateChanged = async item =>
        {
            await _wired.FurniStateSetAsync(item);
            RefreshConfiguration();
        };
        _games.OnPlayingChanged = (entity, playing) => SendTo(entity.Player.Id, new YouArePlayingGameMessageComposer(playing));
        _configuration = ConfigurationStates();
        await _wired.LoadAsync(db, cancellationToken);
        await _wired.LoadVariablesAsync(db, cancellationToken);

        // a tick takes its turn like any call: one interleaved with a call's awaits would find the room
        // changed under it (a tile checked free, then walked onto), and chest coins taken twice
        // one tick at a time, each re-armed by the one before (see TickAsync)
        _tickTimer = this.RegisterGrainTimer(_ => TickAsync(),
            new GrainTimerCreationOptions { DueTime = TickInterval, Period = Timeout.InfiniteTimeSpan, Interleave = false });
        this.RegisterGrainTimer(_ => ShortTickAsync(),
            new GrainTimerCreationOptions { DueTime = ShortTickInterval, Period = ShortTickInterval, Interleave = false });
    }

    public Task<IReadOnlyList<Guid>> GetUserIdsAsync() =>
        Task.FromResult<IReadOnlyList<Guid>>(_entities?.Entities.Select(e => e.Player.Id).ToList() ?? []);

    public Task<RoomInfoData?> GetInfoAsync(Guid requesterId) =>
        Task.FromResult(_room is null || _entities is null
            ? null
            : RoomProjection.ToInfo(_room, _entities.Count, requesterId, RoomGroupInfo, GroupRankOf(requesterId) is not null,
                ActiveEvent is { } ad
                    ? new RoomAdSummary(ad.Name, ad.Description, (int)Math.Ceiling((ad.ExpiresAt - DateTimeOffset.UtcNow).TotalMinutes))
                    : null));

    /// <summary>How many tiles walked are counted towards the walking achievement at once, rather than a call a step.</summary>
    private const int TilesCountedTogether = 50;

    /// <summary>The navigator categories whose rooms count towards each time-spent achievement, by their global keys.</summary>
    private static readonly (string Achievement, string[] Categories)[] RoomTypes =
    [
        (AchievementNames.TimeSpentRoomTypeChat, ["CHAT", "OFFICIAL"]),
        (AchievementNames.TimeSpentRoomTypeGames, ["GAMES"]),
        (AchievementNames.TimeSpentRoomTypeRpg, ["RolePlaying", "Agencies"]),
        (AchievementNames.TimeSpentRoomTypeTrade, ["TRADING"]),
    ];

    private void CountTilesWalked(RoomEntityStore.RoomEntity entity)
    {
        if (entity.TilesWalkedUncounted == 0)
            return;
        GrainFactory.GetGrain<IPlayerGrain>(entity.Player.Id).ProgressAchievementAsync(AchievementNames.LegDay, entity.TilesWalkedUncounted).Ignore();
        entity.TilesWalkedUncounted = 0;
    }

    /// <summary>A user left: their tiles walked, their minutes in a room of its type, and the owner's minutes of hosting.</summary>
    private async Task TimeSpentAchievementsAsync(RoomEntityStore.RoomEntity entity)
    {
        if (entity.Bot is not null || entity.Pet is not null || _room is null)
            return;

        CountTilesWalked(entity);
        var minutes = (int)(DateTimeOffset.UtcNow - entity.EnteredAt).TotalMinutes;
        if (minutes <= 0)
            return;

        if (entity.Player.Id != _room.OwnerId)
            GrainFactory.GetGrain<IPlayerGrain>(_room.OwnerId).ProgressAchievementAsync(AchievementNames.RoomDecoHosting, minutes).Ignore();

        await using var db = await dbFactory.CreateDbContextAsync();
        var key = await db.NavigatorCategories.Where(c => c.Id == _room.CategoryId).Select(c => c.GlobalCategoryKey).FirstOrDefaultAsync();
        if (RoomTypes.FirstOrDefault(type => type.Categories.Contains(key)) is { Achievement: not null } type)
            GrainFactory.GetGrain<IPlayerGrain>(entity.Player.Id).ProgressAchievementAsync(type.Achievement, minutes).Ignore();
    }

    public async Task<RoomLoadData?> EnterAsync(Guid playerId, Guid sessionGuid, PlayerData player)
    {
        if (_room?.Model is null || _model is null || _items is null || _entities is null)
            return null;

        var isReentry = _entities.Find(playerId) is not null;
        // the door answered in a call of its own: a ban, or the room filling up, since then still turns them away
        if (!isReentry && await TurnedAwayAsync(playerId, player))
            return null;
        var previousVisit = isReentry ? null : await PreviousVisitAsync(playerId);
        if (!isReentry)
            await LogVisitAsync(playerId);
        if (!isReentry && playerId != _room.OwnerId)
            GrainFactory.GetGrain<IPlayerGrain>(playerId).ProgressAchievementAsync(AchievementNames.RoomEntry, 1).Ignore();
        var entity = _entities.Enter(playerId, sessionGuid, player, _room.Model.DoorDirection);
        entity.BadgesRank = await badgeLeaderboards.TotalBadgesRankAsync(playerId);
        if (!isReentry)
            PlaceArrival(entity);
        await LoadMembershipAsync(entity);
        entity.ControllerLevel = ControllerLevel(player);
        logger.LogInformation("Player {Player} entered room {RoomId} as entity {Index}",
            player.Username, _room.Id, entity.RoomIndex);

        // Everyone already here learns about the entrant: a brand-new avatar arrives via
        // Users; a re-entering one already exists client-side and just moves to the door.
        // invisible staff are shown to nobody else
        entity.Invisible = invisiblePlayers.IsInvisible(playerId);
        if (!entity.Invisible)
        {
            if (isReentry)
                BroadcastExcept(playerId, UserUpdateMessageComposer.From([_entities.CurrentStatus(entity)]));
            else
                BroadcastExcept(playerId, UsersMessageComposer.From([_entities.ToWireData(entity)]));
        }

        await ReportPopulationAsync();

        _welcomes[playerId] = new PendingWelcome(isReentry, previousVisit);
        return LoadData(player, entity.RoomIndex, entity.ControllerLevel, await CanRateAsync(player.Id));
    }

    public Task ShowOthersToAsync(Guid playerId)
    {
        if (_entities?.Find(playerId) is not { } entity)
            return Task.CompletedTask;

        // avatar by avatar, as Habbo sends them at the end of an entry
        foreach (var other in _entities.Avatars.Where(other => other != entity))
        {
            if (other.Asleep)
                SendTo(playerId, new SleepMessageComposer(other.RoomIndex, Asleep: true));
            if (other.HandItem != 0)
                SendTo(playerId, new CarryObjectMessageComposer(other.RoomIndex, other.HandItem));
            if (other.EffectId != 0)
                SendTo(playerId, new AvatarEffectMessageComposer(other.RoomIndex, other.EffectId, DelayMilliseconds: 0));
            if (other.Pet is null && other.DanceStyle != AvatarActions.NoDance)
                SendTo(playerId, new DanceMessageComposer(other.RoomIndex, other.DanceStyle));
        }

        return Task.CompletedTask;
    }

    /// <summary>Entries whose client does not have the room yet.</summary>
    private readonly Dictionary<Guid, PendingWelcome> _welcomes = [];

    public async Task EnteredAsync(Guid playerId)
    {
        if (!_welcomes.Remove(playerId, out var welcome) || _entities?.Find(playerId) is not { } entity)
            return;

        StepOutOfArrival(entity);
        if (_wired is not null)
            await _wired.UserEnteredAsync(entity);
        if (!welcome.IsReentry)
        {
            await VisitorLoggersGreetAsync(entity, welcome.PreviousVisit);
            await polls.OfferAsync(entity.Player, _room!.Id);
            if (entity.Player.Id == _room.OwnerId && !await settingsRules.MayPickCategoryAsync(entity.Player, _room.CategoryId))
                SendTo(playerId, new Habbo.Protocol.Messages.Outgoing.RoomSettings.ShowEnforceRoomCategoryDialogComposer(
                    Habbo.Protocol.Messages.Outgoing.RoomSettings.ShowEnforceRoomCategoryDialogComposer.AnyCategory));
        }
    }

    /// <summary>The room as the entry sequence shows it to this player.</summary>
    private RoomLoadData LoadData(PlayerData player, int ownRoomIndex, int controllerLevel, bool canRate)
    {
        var room = _room!;
        var wired = WiredRights(player, controllerLevel);
        return new RoomLoadData(
            room.Id,
            room.Name,
            room.Owner?.AccountId ?? 0,
            room.Owner?.Username ?? string.Empty,
            room.OwnerId == player.Id,
            room.Model!.Name,
            room.Model.Heightmap,
            _model!.SizeX,
            _model.SizeY,
            _model.DoorX,
            _model.DoorY,
            room.Model.DoorDirection,
            _entities!.Avatars.Where(e => !e.Invisible || e.Player.Id == player.Id).Select(_entities.ToWireData).ToList(),
            ownRoomIndex,
            Visualization(room, _model),
            _items!.TileMap(),
            _items.Project(ItemType.Floor),
            _items.Project(ItemType.Wall),
            controllerLevel,
            _entities.Avatars.Select(_entities.CurrentStatus).ToList(),
            _items.AreaHides(),
            room.Score,
            canRate,
            ActiveEvent,
            ConfigurationStates(),
            _wired?.ListensForUserClicks ?? false,
            wired.Modify,
            wired.Read);
    }

    public Task LeaveSessionAsync(Guid playerId, Guid sessionGuid) =>
        _entities?.Find(playerId) is { } entity && entity.SessionGuid != sessionGuid ? Task.CompletedTask : LeaveAsync(playerId);

    public async Task LeaveAsync(Guid playerId)
    {
        if (await LeaveQueueOrStandsAsync(playerId) && _entities?.Find(playerId) is null)
            return;

        // leaving the room ends a trade, a ride, and any friendship lock being made
        ForgetFlooding(playerId);
        await CloseTradeAsync(playerId);
        LeaveFriendLocks(playerId);
        await LeaveWiredTradeAsync(playerId);
        await LeaveChestsAsync(playerId);
        if (_entities?.Find(playerId) is { Mount: not null } rider)
            Dismount(rider);

        _welcomes.Remove(playerId);
        var entity = _entities?.Leave(playerId);
        if (entity is null)
            return;
        await TimeSpentAchievementsAsync(entity);

        // out of any team: the gates count one fewer
        if (_games is not null)
            await _games.LeaveAsync(entity);

        Broadcast(new UserRemoveMessageComposer(entity.RoomIndex));
        logger.LogInformation("Player {Player} left room {RoomId}", entity.Player.Username, _room?.Id);

        if (_wired is not null)
            await _wired.UserLeftAsync(entity);
        await ReportPopulationAsync();
        await LetInQueuedPlayersAsync();
        SendQueueStatus();

        if (_entities!.Count == 0 && _spectators.Count == 0 && _queue.Count == 0)
            DeactivateOnIdle();
    }

    /// <summary>Keeps the navigator's user counts live.</summary>
    private Task ReportPopulationAsync() =>
        _room is null || _entities is null
            ? Task.CompletedTask
            : GrainFactory.GetGrain<IRoomPopulationGrain>(0).SetAsync(_room.Id, _entities.Count, GrainContext.Address.SiloAddress);

    public Task<SiloAddress> GetHostAsync() => Task.FromResult(GrainContext.Address.SiloAddress);

    public async Task MoveAsync(Guid playerId, int x, int y)
    {
        if (_entities?.Find(playerId) is not { } entity)
            return;

        await WakeAsync(entity);
        entity.OnArrival = null;

        // riding, it is the pet that goes
        if (entity.Mount is { } mount)
        {
            _entities.Walk(mount, x, y);
            return;
        }

        // staff teleporting go straight to a tile they may stand on
        if (entity.Teleporting)
        {
            if (_model!.IsTile(x, y) && !_items!.BlocksWalking(x, y, clubMember: true, entity.GroupIds) && !_entities.HasAvatarAt(x, y))
                Broadcast(UserUpdateMessageComposer.From([_entities.Warp(entity, x, y)]));
            await DrainEntityChangesAsync();
            return;
        }

        if (_entities.Walk(entity, x, y))
            await DrainEntityChangesAsync();
    }

    public async Task ChatAsync(Guid playerId, string text, int styleId, int trackingId, RoomChatKind kind)
    {
        if (_entities?.Find(playerId) is not { } entity)
            return;

        // the client sends some of its commands as chat; they are done, not said, but count towards flooding
        // like what is said, so commands that show something to the room cannot be sent without pause
        var counted = kind == RoomChatKind.Talk && text.StartsWith(CommandPrefix);
        if (counted && IsFlooding(entity))
            return;
        if (kind == RoomChatKind.Talk && await RoomCommandAsync(entity, text))
            return;

        // a whisper names who it is for, then what it says; one for nobody here goes nowhere
        RoomEntityStore.RoomEntity? recipient = null;
        if (kind == RoomChatKind.Whisper)
        {
            var space = text.IndexOf(' ');
            recipient = space > 0 ? _entities.Entities.FirstOrDefault(e => e != entity
                && string.Equals(e.Player.Username, text[..space], StringComparison.OrdinalIgnoreCase)) : null;
            if (recipient is null)
                return;

            text = text[(space + 1)..];
        }

        if (text.Length > chatOptions.Value.MaxMessageLength)
            text = text[..chatOptions.Value.MaxMessageLength];

        await WakeAsync(entity);

        // a wired mute keeps what the user says from being heard
        if (entity.MutedUntil > DateTime.UtcNow)
            return;

        // and so does staff's, which the user is told the rest of
        if (entity.Player.MutedUntil is { } staffMute && staffMute > DateTimeOffset.UtcNow)
        {
            SendTo(playerId, new RemainingMutePeriodComposer((int)(staffMute - DateTimeOffset.UtcNow).TotalSeconds));
            return;
        }

        // the room's own mute, and the whole room hushed for all but those with rights
        if (_roomMutes.TryGetValue(playerId, out var roomMute) && roomMute > DateTimeOffset.UtcNow)
        {
            SendTo(playerId, new RemainingMutePeriodComposer((int)(roomMute - DateTimeOffset.UtcNow).TotalSeconds));
            return;
        }

        if (_room!.AllInRoomMuted && entity.ControllerLevel < RoomControllerLevels.Rights)
            return;

        if (!counted && IsFlooding(entity))
            return;

        text = (await wordFilter.FilterAsync(text, _filterWords)).Text;
        await LogChatAsync(entity, text);

        var message = new ChatMessage(
            entity.RoomIndex, text, await gestures.DetectAsync(text), await ChatStyleAsync(entity, styleId), Links: [], trackingId);

        // a keyword trigger may hide what it hears: then only the speaker sees it
        if (_wired?.Hides(entity, text) == true)
            SendChat(message, kind, [entity], toSpectators: false);
        else if (recipient is not null)
            SendChat(message, kind, [entity, recipient], toSpectators: false);
        else if (kind == RoomChatKind.Shout)
            SendChat(message, kind, _entities!.Entities, toSpectators: true);
        else
            SendChat(message, kind, WithinHearing(entity), toSpectators: true);

        if (_wired is not null)
            await _wired.UserSaidAsync(entity, text, (int)kind, styleId);

        // bots and pets only answer what was said out loud
        if (recipient is null)
        {
            await BartendersHearAsync(entity, text);
            await PetsHearAsync(entity, text);
        }
    }

    public async Task PlaceFloorItemAsync(Guid playerId, int itemId, int x, int y, int rotation)
    {
        if (_room is null || _items is null || _entities is null || _items.Find(itemId) is not null)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var item = await LoadUnplacedItemAsync(db, itemId, playerId, ItemType.Floor);
        if (item is null || !(IsController(playerId) || RentsTiles(playerId, TilesAt(item.Definition!, x, y, rotation)))
            || !_items.CanPlaceFloorItem(itemId, item.Definition!, x, y, rotation, _entities.HasAvatarAt))
            return;
        if (_items.Items.Count(i => i.Item.Definition!.Type == ItemType.Floor) >= _room.FloorItemLimit)
        {
            SendTo(playerId, FurniLimitReached());
            return;
        }

        // the row first: an item the database did not take is not put down in memory either
        var data = await furniData.LoadAsync(db, item);
        item.RoomId = _room.Id;
        item.X = x;
        item.Y = y;
        item.Z = _items.SurfaceAt(x, y);
        item.Rotation = rotation;
        await db.SaveChangesAsync();
        var placed = ShowPlacedFloorItem(item, data);
        SendTo(playerId, FurniListRemoveComposer.Item(itemId));
        await FloorItemPlacedAsync(playerId, placed);
    }

    /// <summary>A floor item the database already has at its place, put down in memory and shown to everyone.</summary>
    private RoomItemStore.PlacedItem ShowPlacedFloorItem(UserItem item, FurniDataView data)
    {
        var placed = _items!.Add(item, data, item.X!.Value, item.Y!.Value, item.Rotation!.Value);
        var reshaped = _items.RefreshWaterAreas();

        Broadcast(new ObjectAddMessageComposer(FloorItemData.From(RoomItemStore.ToWireData(placed), withOwnerName: true)));
        BroadcastHeights(RoomItemStore.Footprint(placed));
        BroadcastWaterAreas(reshaped, item.Id);
        return placed;
    }

    /// <summary>What a floor item put down sets off: a chest locks, wired hears of it, the owner's achievements.</summary>
    private async Task FloorItemPlacedAsync(Guid playerId, RoomItemStore.PlacedItem placed)
    {
        await LockPlacedChestAsync(placed);
        if (_wired is not null)
            await _wired.PlacedAsync(placed);
        RefreshConfiguration();
        RoomBuilderAchievements(playerId);
    }

    /// <summary>The logic Habbo's furnidata gives floor holes.</summary>
    private const string FloorHoleLogic = "furniture_floor_hole";

    /// <summary>The owner put furni down in their room: how much, how many kinds, the credit furni's worth, the holes.
    /// Borrowed Builders Club furni counts for none of it: 500 of it, free, would buy the lot.</summary>
    private void RoomBuilderAchievements(Guid playerId)
    {
        if (_room is null || _items is null || playerId != _room.OwnerId)
            return;

        var items = _items.Items.Where(item => !item.Temporary && !item.Item.BuildersClub).Select(item => item.Item.Definition!).ToList();
        var owner = GrainFactory.GetGrain<IPlayerGrain>(playerId);
        owner.ReachAchievementAsync(AchievementNames.RoomDecoFurniCount, items.Count).Ignore();
        owner.ReachAchievementAsync(AchievementNames.RoomDecoFurniTypeCount, items.Select(d => d.Id).Distinct().Count()).Ignore();
        owner.ReachAchievementAsync(AchievementNames.PlaceCreditValue, items.Sum(d => d.CreditValue ?? 0)).Ignore();
        owner.ReachAchievementAsync(AchievementNames.RoomDecoHoleFurniCount, items.Count(d => d.Logic == FloorHoleLogic)).Ignore();
    }

    public async Task PlaceWallItemAsync(Guid playerId, int itemId, string wallLocation)
    {
        if (_room is null || _items is null || _items.Find(itemId) is not null || !IsController(playerId)
            || !WallLocation.TryParse(wallLocation, _model!, out var location))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var item = await LoadUnplacedItemAsync(db, itemId, playerId, ItemType.Wall);
        if (item is null)
            return;
        if (_items.Items.Count(i => i.Item.Definition!.Type == ItemType.Wall) >= _room.WallItemLimit)
        {
            SendTo(playerId, FurniLimitReached());
            return;
        }

        item.RoomId = _room.Id;
        item.WallPosition = location.ToString();
        var data = await furniData.LoadAsync(db, item);
        await db.SaveChangesAsync();
        ShowPlacedWallItem(item, data);
        SendTo(playerId, FurniListRemoveComposer.Item(itemId));
        RoomBuilderAchievements(playerId);
    }

    private void ShowPlacedWallItem(UserItem item, FurniDataView data) =>
        Broadcast(new ItemAddMessageComposer(WallItemData.From(RoomItemStore.ToWireData(_items!.AddWall(item, data)), withOwnerName: true)));

    public async Task MoveFloorItemAsync(Guid playerId, int itemId, int x, int y, int rotation)
    {
        if (_items?.Find(itemId) is not { } placed)
            return;

        // a renter moves their own furniture about the space they rent
        var renterMove = placed.Item.OwnerId == playerId && RentsTiles(playerId, RoomItemStore.Footprint(placed))
                         && RentsTiles(playerId, TilesAt(placed.Item.Definition!, x, y, rotation));
        if (IsController(playerId) || renterMove)
            await MovePlacedFloorItemAsync(placed, x, y, rotation, wired: null);
    }

    /// <summary>Shows everyone the stacking heights of tiles that changed, in as many messages as they take.</summary>
    private void BroadcastHeights(IEnumerable<(int X, int Y)> tiles)
    {
        foreach (var update in HeightMapUpdateMessageComposer.Chunked(_items!.TileUpdates(tiles)))
            Broadcast(update);
    }

    /// <summary>Shows everyone the tiles, of those noted before a change, whose stacking height or flag it changed:
    /// Habbo sends a turned bed's four changed tiles, not its old and new footprints whole.</summary>
    private void BroadcastChangedHeights(IReadOnlyList<HeightMapTileData> before)
    {
        var changed = _items!.TileUpdates(before.Select(tile => (tile.X, tile.Y))).Except(before).ToList();
        foreach (var update in HeightMapUpdateMessageComposer.Chunked(changed))
            Broadcast(update);
    }

    /// <summary>Moves and turns a floor item, if it may stand there, and shows everyone. Players
    /// moving furniture and wired moving it go the same way.</summary>
    /// <summary>Moves a floor item, for its owner's hand or for wired. Wired moves go by the stack's
    /// options: the client animates them, they may pass through what would stop a hand, and they
    /// may carry the users on the furni along.</summary>
    /// <summary>Moves a floor item, as a hand, wired (<paramref name="wired"/>) or a game moves one. <paramref name="intoFurni"/>
    /// lets it onto furniture at the floor's height, as a football goes into a goal's net; <paramref name="anyDirection"/>
    /// lets it face a way its artwork lacks, as a rolling ball does.</summary>
    private async Task<bool> MovePlacedFloorItemAsync(RoomItemStore.PlacedItem placed, int x, int y, int rotation, WiredMoveOptions? wired,
        bool intoFurni = false, bool anyDirection = false)
    {
        var itemId = placed.Item.Id;
        // a hand moves and turns furniture onto whoever stands there (Habbo: a bed turned onto a standing avatar, a
        // sandwich moved onto one, both went); wired goes by its stack's option
        var avatarsBlock = wired?.AvatarsBlock ?? false;
        Func<int, int, bool> avatarAt = avatarsBlock && _entities is not null ? _entities.HasAvatarAt : (_, _) => false;
        if (_items is null || _entities is null || _items.Find(itemId) != placed
            || placed.Item.Definition!.Type != ItemType.Floor
            || !_items.CanPlaceFloorItem(itemId, placed.Item.Definition, x, y, rotation, avatarAt,
                (wired?.ThroughFurni ?? false) || intoFurni, wired?.BlockedByFurni ?? false, anyDirection))
            return false;

        // the row first: an item the database no longer has in this room is not moved in memory either; wired's
        // temporary copies have none
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = placed.Temporary ? null : await db.UserItems.FirstOrDefaultAsync(i => i.Id == itemId && i.RoomId == _room!.Id);
        if (row is null && !placed.Temporary)
            return false;

        var vacated = RoomItemStore.Footprint(placed).ToList();
        // the tiles it moves onto first, then those it leaves, as Habbo lists them
        var heightsBefore = _items.TileUpdates(TilesAt(placed.Item.Definition, x, y, rotation).Concat(vacated));
        var (fromX, fromY, fromZ) = (placed.X, placed.Y, placed.Z);
        var carried = wired?.CarryMode is { } carryMode ? Riders(placed, carryMode) : [];
        _items.MoveTo(placed, x, y, rotation, wired is { KeepAltitude: true } || intoFurni ? placed.Z : null);
        var reshaped = _items.RefreshWaterAreas();

        _wired?.Moved();

        if (row is not null)
        {
            row.X = x;
            row.Y = y;
            row.Z = placed.Z;
            row.Rotation = rotation;
            await db.SaveChangesAsync();
        }

        if (wired is null)
        {
            Broadcast(new ObjectUpdateMessageComposer(FloorItemData.From(RoomItemStore.ToWireData(placed), withOwnerName: false)));
        }
        else
        {
            var movements = new List<WiredMovementData>
            {
                new WiredFurniMoveData(fromX, fromY, x, y, RoomItemStore.FormatHeight(fromZ), RoomItemStore.FormatHeight(placed.Z),
                    itemId, wired.AnimationMilliseconds, rotation, wired.OvershootMilliseconds, wired.CurveStrength),
            };
            foreach (var rider in carried)
            {
                var (riderX, riderY, riderZ) = (rider.X, rider.Y, rider.Z);
                _entities.Carry(rider, rider.X + x - fromX, rider.Y + y - fromY);
                movements.Add(new WiredUserMoveData(riderX, riderY, rider.X, rider.Y,
                    RoomItemStore.FormatHeight(riderZ), RoomItemStore.FormatHeight(rider.Z), rider.RoomIndex,
                    WiredUserMoveData.Slide, wired.AnimationMilliseconds, rider.BodyDirection, rider.HeadDirection, wired.CurveStrength));
            }

            Broadcast(new WiredMovementsMessageComposer(movements));
        }

        // whoever sits or lies where it was or now is turns and settles with it, as when a chair is rotated under them
        ResettleOn(vacated.Concat(RoomItemStore.Footprint(placed)), carried);

        BroadcastChangedHeights(heightsBefore);
        BroadcastWaterAreas(reshaped, itemId);
        return true;
    }

    /// <summary>Everyone sitting or lying on the tiles, but those carried, settled again on what is under them now and
    /// shown to the room: turned with a rotated seat, or standing up when it was taken away.</summary>
    private void ResettleOn(IEnumerable<(int X, int Y)> tiles, IReadOnlyCollection<RoomEntityStore.RoomEntity> carried)
    {
        var affected = tiles.ToHashSet();
        var resettled = _entities!.Entities
            .Where(entity => affected.Contains((entity.X, entity.Y)) && !carried.Contains(entity))
            .Select(_entities.Resettle)
            .OfType<AvatarUpdateData>()
            .ToList();
        if (resettled.Count > 0)
            Broadcast(UserUpdateMessageComposer.From(resettled));
    }

    /// <summary>The users a moving furni carries: those standing on it, or everyone on its tiles.</summary>
    private List<RoomEntityStore.RoomEntity> Riders(RoomItemStore.PlacedItem placed, int carryMode)
    {
        const double tolerance = 0.01;
        var tiles = RoomItemStore.Footprint(placed).ToHashSet();
        return _entities!.Entities
            .Where(user => tiles.Contains((user.X, user.Y)))
            .Where(user => carryMode == Wired.Addons.CarryUsersAddon.OnItsTiles
                           || Math.Abs(user.Z - RoomItemStore.TopOf(placed)) < tolerance)
            .ToList();
    }

    public async Task MoveWallItemAsync(Guid playerId, int itemId, string wallLocation)
    {
        if (!IsController(playerId) || _items?.Find(itemId) is not { } placed || placed.Item.Definition!.Type != ItemType.Wall
            || !WallLocation.TryParse(wallLocation, _model!, out var location))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.UserItems.FirstOrDefaultAsync(i => i.Id == itemId && i.RoomId == _room!.Id) is not { } row)
            return;

        row.WallPosition = location.ToString();
        placed.Item.WallPosition = row.WallPosition;
        await db.SaveChangesAsync();

        Broadcast(new ItemUpdateMessageComposer(WallItemData.From(RoomItemStore.ToWireData(placed), withOwnerName: false)));
    }

    public async Task PickupItemAsync(Guid playerId, int itemId, bool confirmed)
    {
        // wired's temporary copies belong to no one's inventory: only wired removes them
        if (_items?.Find(itemId) is not { Temporary: false } placed || _entities?.Find(playerId) is not { } picker)
            return;

        // the owner, staff and a group's admins pick up anything; those with rights only their own, and so
        // does a renter on the space they rent
        if (!MayPickUp(picker, placed))
            return;

        // a variable takes its values and every reference to it along, so the player says yes first
        if (!confirmed && wiredBoxes.For(placed.Item.Definition!.ClassName) is Habbo.Silo.Wired.Variables.WiredVariableBox)
        {
            SendTo(playerId, new ObjectRemoveConfirmMessageComposer(
                placed.Item.Definition.Type == ItemType.Wall ? WallPickup : FloorPickup, itemId, VariablePickupTitle, VariablePickupBody));
            return;
        }

        await ReturnToOwnerAsync(placed, picker.Player.AccountId);
    }

    /// <summary>The owner, staff and a group's admins pick up anything; those with rights only their own, and so
    /// does a renter on the space they rent.</summary>
    private bool MayPickUp(RoomEntityStore.RoomEntity picker, RoomItemStore.PlacedItem placed)
    {
        var playerId = picker.Player.Id;
        var renterPickup = placed.Item.OwnerId == playerId && RentsTiles(playerId, RoomItemStore.Footprint(placed));
        return renterPickup || (picker.ControllerLevel >= RoomControllerLevels.Rights
            && !(picker.ControllerLevel is RoomControllerLevels.Rights or RoomControllerLevels.GroupRights && placed.Item.OwnerId != playerId));
    }

    /// <summary>Takes an item out of the room and back into its owner's inventory.</summary>
    private Task ReturnToOwnerAsync(RoomItemStore.PlacedItem placed, int pickerAccountId) =>
        ReturnToOwnersAsync([placed], pickerAccountId);

    /// <summary>Takes items out of the room, each back into its owner's inventory; the room sees the floor items go
    /// all at once.</summary>
    private async Task ReturnToOwnersAsync(IReadOnlyList<RoomItemStore.PlacedItem> placed, int pickerAccountId)
    {
        if (placed.Count == 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var ids = placed.Select(p => p.Item.Id).ToList();
        var rows = await db.UserItems.Where(i => ids.Contains(i.Id) && i.RoomId == _room!.Id).ToDictionaryAsync(i => i.Id);
        var taken = placed.Where(p => rows.ContainsKey(p.Item.Id)).ToList();
        if (taken.Count == 0)
            return;

        // the rows first: items the database did not take back stay in the room in memory too. Borrowed Builders
        // Club furni goes back to the warehouse, which is to say it is gone.
        foreach (var row in rows.Values.Where(row => row.BuildersClub))
            db.UserItems.Remove(row);
        foreach (var row in rows.Values.Where(row => !row.BuildersClub))
        {
            row.RoomId = null;
            row.X = row.Y = null;
            row.Z = null;
            row.Rotation = null;
            row.WallPosition = null;
        }

        await db.SaveChangesAsync();

        var floor = taken.Where(p => p.Item.Definition!.Type != ItemType.Wall).ToList();
        var vacated = floor.SelectMany(RoomItemStore.Footprint).Distinct().ToList();
        foreach (var item in taken)
        {
            var itemId = item.Item.Id;
            _items!.Remove(itemId);
            ForgetChest(itemId);
            EmptyBreedingNest(itemId);
            if (_wired is not null)
                await _wired.RemovedAsync(itemId);
        }

        var reshaped = _items!.RefreshWaterAreas();

        foreach (var wall in taken.Where(p => p.Item.Definition!.Type == ItemType.Wall))
            Broadcast(new ItemRemoveMessageComposer(wall.Item.Id, pickerAccountId));
        if (floor.Count == 1)
            Broadcast(new ObjectRemoveMessageComposer(floor[0].Item.Id, IsExpired: false, pickerAccountId, Delay: 0));
        else if (floor.Count > 1)
            Broadcast(new ObjectRemoveMultipleMessageComposer(floor.Select(p => p.Item.Id).ToList(), pickerAccountId));
        if (vacated.Count > 0)
            BroadcastHeights(vacated);
        ResettleOn(vacated, []);

        var takenIds = taken.Select(p => p.Item.Id).ToHashSet();
        foreach (var item in reshaped.Where(item => !takenIds.Contains(item.Item.Id)))
            Broadcast(new ObjectDataUpdateMessageComposer(item.Item.Id, FurniStuffData.From(item.Data)));
        RefreshConfiguration();

        // The room already holds the items' data, so the inventory entries are built from that
        // rather than read back from the tables they were loaded from. Each goes back to whoever
        // owns it, wherever they are.
        foreach (var borrower in taken.Where(p => rows[p.Item.Id].BuildersClub).Select(p => rows[p.Item.Id].OwnerId).Distinct())
            await SendBuildersClubCountAsync(db, borrower);
        foreach (var owner in taken.Where(p => !rows[p.Item.Id].BuildersClub).GroupBy(p => rows[p.Item.Id].OwnerId))
        {
            var returned = new FurniListAddOrUpdateComposer(owner
                .Select(p => InventoryFurniData.From(InventoryProjection.From(rows[p.Item.Id], p.Item.Definition!, p.Data))).ToList());
            await GrainFactory.GetGrain<IPlayerGrain>(owner.Key).SendAsync(returned.ToMessage());
        }
    }

    /// <summary>The bubble that says the room holds all the furni it may.</summary>
    private static NotificationDialogMessageComposer FurniLimitReached() =>
        new(FurniPlacementErrorType, [new NotificationParameterData(NotificationMessageKey, MaxFurnitureText)]);

    private const string FurniPlacementErrorType = "furni_placement_error";
    private const string NotificationMessageKey = "message";
    private const string MaxFurnitureText = "${room.error.max_furniture}";

    // ---------- the wired menu ----------

    /// <summary>The wired time zones the hotel offers (the client's wired.timezones), the first
    /// being the hotel's own.</summary>
    private const string WiredTimezonesKey = "wired.timezones";

    /// <summary>The most log lines a room keeps; older ones go.</summary>
    private const int MaxWiredLogLines = 2000;

    private const int MaxWiredLogPageSize = 100;

    /// <summary>How the log shows when a line was written, in the room's time zone.</summary>
    private const string WiredLogTimeFormat = "yyyy-MM-dd HH:mm:ss";

    private async Task<IReadOnlyList<string>> WiredTimezonesAsync() =>
        (await clientVariables.GetAsync(WiredTimezonesKey) ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>The room's wired time zone: its own, or the hotel's.</summary>
    private async Task<string> WiredTimezoneAsync() =>
        _room!.WiredTimezone is { Length: > 0 } zone ? zone : (await WiredTimezonesAsync()).FirstOrDefault() ?? string.Empty;

    /// <summary>Whether the user may change, and read, the room's wired: the owner and staff always;
    /// anyone else by the permission levels they hold and the room's settings.</summary>
    private (bool Modify, bool Read) WiredRights(RoomEntityStore.RoomEntity entity) =>
        WiredRights(entity.Player, entity.ControllerLevel);

    /// <summary>The same for a player by their controller level, whether or not they have an avatar here (a
    /// spectator has none).</summary>
    private (bool Modify, bool Read) WiredRights(PlayerData player, int controllerLevel)
    {
        if (_room is null)
            return (false, false);
        if (_room.OwnerId == player.Id || permissions.Has(player, PermissionNames.WiredManageAny))
            return (true, true);

        var held = 1 << WiredPermissionLevels.Everyone;
        if (controllerLevel >= RoomControllerLevels.Rights)
            held |= 1 << WiredPermissionLevels.UsersWithRights;
        // what the user is to the room's group, which the settings offer as levels of their own
        if (GroupRankOf(player.Id) is { } rank)
        {
            held |= 1 << WiredPermissionLevels.GroupMembers;
            if (rank is GroupRank.Owner or GroupRank.Admin)
                held |= 1 << WiredPermissionLevels.GroupAdmins;
        }

        var modify = (_room.WiredModifyPermissionMask & held) != 0;
        return (modify, modify || (_room.WiredReadPermissionMask & held) != 0);
    }

    private WiredPermissionsComposer WiredPermissions(RoomEntityStore.RoomEntity entity)
    {
        var (modify, read) = WiredRights(entity);
        return new WiredPermissionsComposer(modify, read);
    }

    /// <summary>The user, if here and allowed to read the room's wired.</summary>
    private RoomEntityStore.RoomEntity? WiredReader(Guid playerId) =>
        _entities?.Find(playerId) is { } entity && WiredRights(entity).Read ? entity : null;

    public Task GetWiredSettingsAsync(Guid playerId)
    {
        if (WiredReader(playerId) is not null)
            SendTo(playerId, new WiredRoomSettingsComposer(_room!.WiredModifyPermissionMask, _room.WiredReadPermissionMask, _room.WiredTimezone));
        return Task.CompletedTask;
    }

    public async Task SetWiredSettingsAsync(Guid playerId, int modifyPermissionMask, int readPermissionMask, string timezone)
    {
        if (_room is null || _entities?.Find(playerId) is not { } entity
            || (_room.OwnerId != playerId && !permissions.Has(entity.Player, PermissionNames.WiredManageAny)))
            return;

        // only the levels the settings offer, and a zone the hotel lists
        const int offered = (1 << WiredPermissionLevels.Everyone) | (1 << WiredPermissionLevels.UsersWithRights)
                            | (1 << WiredPermissionLevels.GroupMembers) | (1 << WiredPermissionLevels.GroupAdmins);
        if (timezone.Length > 0 && !(await WiredTimezonesAsync()).Contains(timezone))
            timezone = _room.WiredTimezone;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Rooms.FirstOrDefaultAsync(r => r.Id == _room.Id) is not { } row)
            return;

        _room.WiredModifyPermissionMask = row.WiredModifyPermissionMask = modifyPermissionMask & offered;
        _room.WiredReadPermissionMask = row.WiredReadPermissionMask = readPermissionMask & offered;
        _room.WiredTimezone = row.WiredTimezone = timezone;
        await db.SaveChangesAsync();
        _wired!.Timezone = _games!.Timezone = await WiredTimezoneAsync();

        SendTo(playerId, new WiredRoomSettingsComposer(_room.WiredModifyPermissionMask, _room.WiredReadPermissionMask, _room.WiredTimezone));
        foreach (var other in _entities.Entities)
            SendTo(other.Player.Id, WiredPermissions(other));
    }

    public async Task GetWiredLogsAsync(Guid playerId, WiredLogQuery query)
    {
        if (WiredReader(playerId) is null || _room is null || _wired is null)
            return;

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, MaxWiredLogPageSize);
        byte? level = query.LevelFilter >= 0 ? (byte)query.LevelFilter : null;
        byte? source = query.SourceFilter >= 0 ? (byte)query.SourceFilter : null;
        var text = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query;

        await using var db = await dbFactory.CreateDbContextAsync();
        var lines = db.WiredRoomLogs.AsNoTracking().Where(l => l.RoomId == _room.Id);
        if (level is { } l1)
            lines = lines.Where(l => l.Level == l1);
        if (source is { } s1)
            lines = lines.Where(l => l.Source == s1);
        if (text is not null)
            lines = lines.Where(l => EF.Functions.ILike(l.Message, "%" + text + "%"));

        var total = await lines.CountAsync();
        var found = await lines.OrderByDescending(l => l.Id).Skip((page - 1) * size).Take(size).ToListAsync();
        var zone = WiredZone(_wired.Timezone);
        SendTo(playerId, new WiredRoomLogsComposer(total, page, size,
            found.Select(l => new WiredLogEntryData(l.Id, l.Level, l.Source, l.Message, l.CreatedAt.ToUnixTimeMilliseconds(),
                TimeZoneInfo.ConvertTime(l.CreatedAt, zone).ToString(WiredLogTimeFormat, System.Globalization.CultureInfo.InvariantCulture))).ToList(),
            level, source, text));
    }

    private static TimeZoneInfo WiredZone(string zone) =>
        zone.Length > 0 && TimeZoneInfo.TryFindSystemTimeZoneById(zone, out var found) ? found : TimeZoneInfo.Utc;

    // how many permanent variables of each kind a room may have, as Habbo's wired monitor reports them
    private const int MaxPermanentFurniVariables = 21;
    private const int MaxPermanentUserVariables = 87;
    private const int MaxPermanentGlobalVariables = 65;

    private int PermanentVariables(int target) =>
        _wired!.Variables.Count(v => v.Kind == Wired.Variables.WiredVariableKinds.CreatedByUser && v.Target == target
                                     && Wired.Variables.WiredVariableAvailability.IsPermanent(v.Availability));

    public Task GetWiredStatsAsync(Guid playerId)
    {
        if (WiredReader(playerId) is null || _room is null || _items is null || _wired is null)
            return Task.CompletedTask;

        var (cost, cap, heavy) = _wired.Usage;
        SendTo(playerId, new WiredRoomStatsComposer(cost, cap, heavy,
            _items.Items.Count(i => i.Item.Definition!.Type == ItemType.Floor), _room.FloorItemLimit,
            _items.Items.Count(i => i.Item.Definition!.Type == ItemType.Wall), _room.WallItemLimit,
            PermanentVariables(Wired.Variables.WiredVariableTargets.Furni), MaxPermanentFurniVariables,
            PermanentVariables(Wired.Variables.WiredVariableTargets.User), MaxPermanentUserVariables,
            PermanentVariables(Wired.Variables.WiredVariableTargets.Global), MaxPermanentGlobalVariables));
        return Task.CompletedTask;
    }

    public Task GetWiredErrorsAsync(Guid playerId)
    {
        if (WiredReader(playerId) is null || _wired is null)
            return Task.CompletedTask;

        var now = DateTimeOffset.UtcNow;
        SendTo(playerId, new WiredErrorLogsComposer(_wired.ErrorCounts
            .Select(e => new WiredErrorData(e.Kind.Id, e.Kind.Name, e.Kind.Category, e.Count,
                e.Last is { } last ? (long)(now - last).TotalMilliseconds : Wired.WiredErrors.NeverHappened))
            .ToList()));
        return Task.CompletedTask;
    }

    public Task ClearWiredErrorsAsync(Guid playerId)
    {
        if (_entities?.Find(playerId) is not { } entity || !WiredRights(entity).Modify || _wired is null)
            return Task.CompletedTask;

        _wired.ClearErrors();
        return GetWiredErrorsAsync(playerId);
    }

    public Task GetWiredVariablesHashAsync(Guid playerId)
    {
        if (WiredReader(playerId) is not null && _wired is not null)
            SendTo(playerId, new WiredAllVariablesHashComposer(_wired.VariablesHash));
        return Task.CompletedTask;
    }

    public Task GetWiredVariablesDiffsAsync(Guid playerId, IReadOnlyList<KnownWiredVariable> known)
    {
        if (WiredReader(playerId) is null || _wired is null)
            return Task.CompletedTask;

        var byId = new Dictionary<string, int>();
        foreach (var variable in known)
            byId[variable.Id] = variable.Hash;
        foreach (var chunk in _wired.VariableDiffs(byId))
            SendTo(playerId, chunk);
        return Task.CompletedTask;
    }

    // ---------- the wired menu's variable tools ----------

    /// <summary>The entity type the variable tools give a Habbo (wiredmenu.variable_management.usertype.1).</summary>
    private const int HabboEntityType = 1;

    /// <summary>Any user type, in the owners page's filter.</summary>
    private const int AnyUserType = -1;

    // wiredmenu.variable_management.sort_by.N
    private const int SortHighestValue = 0;
    private const int SortLowestValue = 1;
    private const int SortOldestCreation = 2;
    private const int SortLatestCreation = 3;
    private const int SortOldestUpdate = 4;

    private const int MaxOwnersPageSize = 100;

    /// <summary>The user, if here and allowed to change the room's wired.</summary>
    private RoomEntityStore.RoomEntity? WiredModifier(Guid playerId) =>
        _entities?.Find(playerId) is { } entity && WiredRights(entity).Modify ? entity : null;

    private string WiredTime(DateTimeOffset time) =>
        TimeZoneInfo.ConvertTime(time, WiredZone(_wired!.Timezone)).ToString(WiredLogTimeFormat, System.Globalization.CultureInfo.InvariantCulture);

    public Task SharedVariableChangedAsync(SharedVariableChange change) =>
        _wired?.SharedVariableChangedAsync(change) ?? Task.CompletedTask;

    public Task GetWiredVariablesForObjectAsync(Guid playerId, int target, int objectId)
    {
        if (WiredReader(playerId) is null || _wired is null)
            return Task.CompletedTask;
        if (_wired.HolderOf(target, objectId) is not { } holder)
        {
            SendTo(playerId, new Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu.WiredMenuErrorComposer(
                Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu.WiredMenuErrorComposer.ObjectNotFound));
            return Task.CompletedTask;
        }

        SendTo(playerId, new WiredVariablesForObjectComposer(target,
            target == WiredVariablesForObjectComposer.FurniTarget ? objectId : null,
            target == WiredVariablesForObjectComposer.UserTarget ? objectId : null,
            _wired.HeldVariables(holder).Select(v => new WiredVariableValueData(v.Id, v.Value)).ToList(),
            target == WiredVariablesForObjectComposer.FurniTarget ? _wired.BoxesPicking(objectId) : null));
        return Task.CompletedTask;
    }

    public async Task EditWiredVariableAsync(Guid playerId, WiredVariableEdit edit)
    {
        if (WiredModifier(playerId) is null || _wired?.HolderOf(edit.Target, edit.ObjectId) is not { } holder
            || _wired.FindVariable(edit.VariableId) is not { } variable)
            return;

        await _wired.EditVariableAsync(variable, holder, edit.Value, edit.Action);
        await GetWiredVariablesForObjectAsync(playerId, edit.Target, edit.ObjectId);
    }

    public Task GetWiredVariableHoldersAsync(Guid playerId, string variableId)
    {
        if (WiredReader(playerId) is null || _wired?.FindVariable(variableId) is not { } variable)
            return Task.CompletedTask;

        SendTo(playerId, new WiredAllVariableHoldersComposer(variable.Target, variable.ToWire(),
            _wired.VariableHolders(variable)
                .Select(h => new WiredVariableHolderData(h.Holder.Item?.Item.Id ?? h.Holder.User?.RoomIndex ?? 0, h.Value))
                .ToList()));
        return Task.CompletedTask;
    }

    /// <summary>The permanent user variable the variable tools may manage.</summary>
    private Wired.Variables.WiredVariable? PermanentUserVariable(string variableId) =>
        _wired?.FindVariable(variableId) is { Target: Wired.Variables.WiredVariableTargets.User } variable
        && Wired.Variables.WiredVariableAvailability.IsPermanent(variable.Availability)
            ? variable
            : null;

    public async Task GetWiredVariableOwnersAsync(Guid playerId, WiredOwnersQuery query)
    {
        if (WiredReader(playerId) is null || _room is null || PermanentUserVariable(query.VariableId) is not { } variable)
            return;

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, MaxOwnersPageSize);

        await using var db = await dbFactory.CreateDbContextAsync();
        var held = db.WiredVariableValues.AsNoTracking()
            .Where(v => v.RoomId == _room.Id && v.VariableId == variable.Id && v.UserId != null);

        // everyone who can hold a user variable here is a Habbo, so any other type finds nobody
        if (query.UserTypeFilter != AnyUserType && query.UserTypeFilter != HabboEntityType)
            held = held.Where(_ => false);

        var sorted = query.SortType switch
        {
            SortHighestValue => held.OrderByDescending(v => v.Value),
            SortLowestValue => held.OrderBy(v => v.Value),
            SortOldestCreation => held.OrderBy(v => v.CreatedAt),
            SortLatestCreation => held.OrderByDescending(v => v.CreatedAt),
            SortOldestUpdate => held.OrderBy(v => v.UpdatedAt),
            _ => held.OrderByDescending(v => v.UpdatedAt),
        };

        var total = await held.CountAsync();
        var rows = await sorted.ThenBy(v => v.Id).Skip((page - 1) * size).Take(size)
            .Join(db.Users, v => v.UserId, u => u.Id, (v, u) => new { v.Value, v.CreatedAt, v.UpdatedAt, u.AccountId, u.Username })
            .ToListAsync();

        SendTo(playerId, new WiredUserVariablesListComposer(variable.Id, total, page, size,
            rows.Select(r => new WiredUserVariablesElementData(HabboEntityType, r.AccountId, r.Username,
                new WiredVariableStorageData(r.Value, r.CreatedAt.ToUnixTimeMilliseconds(), WiredTime(r.CreatedAt),
                    r.UpdatedAt.ToUnixTimeMilliseconds(), WiredTime(r.UpdatedAt)))).ToList(),
            query.UserTypeFilter, query.SortType));
    }

    public async Task GetWiredUserPermanentVariablesAsync(Guid playerId, int entityType, int entityId)
    {
        if (WiredReader(playerId) is null || _room is null || _wired is null || entityType != HabboEntityType)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == entityId) is not { } user)
            return;

        var permanent = _wired.Variables
            .Where(v => v.Target == Wired.Variables.WiredVariableTargets.User && Wired.Variables.WiredVariableAvailability.IsPermanent(v.Availability))
            .Select(v => v.Id).ToList();
        var rows = await db.WiredVariableValues.AsNoTracking()
            .Where(v => v.RoomId == _room.Id && v.UserId == user.Id && permanent.Contains(v.VariableId))
            .ToListAsync();

        SendTo(playerId, new WiredUserPermanentVariablesComposer(HabboEntityType, user.AccountId, user.Username, user.Figure,
            null, null, null,
            rows.Select(r => new WiredNamedVariableStorageData(r.VariableId, r.Value,
                r.CreatedAt.ToUnixTimeMilliseconds(), WiredTime(r.CreatedAt),
                r.UpdatedAt.ToUnixTimeMilliseconds(), WiredTime(r.UpdatedAt))).ToList()));
    }

    public async Task EditWiredUserPermanentVariableAsync(Guid playerId, int entityType, WiredVariableEdit edit)
    {
        if (WiredModifier(playerId) is null || _room is null || _wired is null)
            return;

        var done = entityType == HabboEntityType && PermanentUserVariable(edit.VariableId) is { } variable
                   && await EditPermanentUserValueAsync(variable, edit);
        SendTo(playerId, new WiredSetUserPermanentVariableResultComposer(done));
        if (done)
            await GetWiredUserPermanentVariablesAsync(playerId, entityType, edit.ObjectId);
    }

    /// <summary>Changes a user's kept value: through the room's wired when they are here, so it hears
    /// of it; straight in the database when they are not.</summary>
    private async Task<bool> EditPermanentUserValueAsync(Wired.Variables.WiredVariable variable, WiredVariableEdit edit)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == edit.ObjectId) is not { } user)
            return false;

        if (_entities?.Find(user.Id) is { } entity)
            return await _wired!.EditVariableAsync(variable, Wired.Variables.WiredHolder.Of(entity), edit.Value, edit.Action);

        var row = await db.WiredVariableValues.FirstOrDefaultAsync(v =>
            v.RoomId == _room!.Id && v.VariableId == variable.Id && v.UserId == user.Id && v.ItemId == null);
        var now = DateTimeOffset.UtcNow;
        switch (edit.Action)
        {
            case WiredVariableMenuActions.Set when row is not null && variable.HasValue:
                row.Value = edit.Value;
                row.UpdatedAt = now;
                break;
            case WiredVariableMenuActions.Give when row is null:
                db.WiredVariableValues.Add(new Habbo.Domain.Furniture.Data.WiredVariableValueRow
                {
                    RoomId = _room!.Id, VariableId = variable.Id, UserId = user.Id,
                    Value = variable.HasValue ? edit.Value : 0, CreatedAt = now, UpdatedAt = now,
                });
                break;
            case WiredVariableMenuActions.Give when variable.HasValue:
                row!.Value = edit.Value;
                row.UpdatedAt = now;
                break;
            case WiredVariableMenuActions.Remove when row is not null:
                db.WiredVariableValues.Remove(row);
                break;
            default:
                return false;
        }

        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>Writes a line of the room's wired log, keeping only the newest lines.</summary>
    /// <summary>How many lines the wired log takes between trims, and how many it has taken.</summary>
    private const int WiredLogTrimEvery = 50;
    private int _wiredLogWrites;

    private async Task WriteWiredLogAsync(byte level, byte source, string message)
    {
        if (_room is null)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        db.WiredRoomLogs.Add(new WiredRoomLog
        {
            RoomId = _room.Id,
            Level = level,
            Source = source,
            Message = message.Length > WiredRoomLog.MessageMaxLength ? message[..WiredRoomLog.MessageMaxLength] : message,
        });
        await db.SaveChangesAsync();

        // the log is cut back to its length now and then, not after every line a repeater writes
        if (++_wiredLogWrites % WiredLogTrimEvery != 0)
            return;

        var oldestKept = await db.WiredRoomLogs.Where(l => l.RoomId == _room.Id)
            .OrderByDescending(l => l.Id).Skip(MaxWiredLogLines - 1).Select(l => (long?)l.Id).FirstOrDefaultAsync();
        if (oldestKept is { } oldest)
            await db.WiredRoomLogs.Where(l => l.RoomId == _room.Id && l.Id < oldest).ExecuteDeleteAsync();
    }

    // ---------- rights ----------

    /// <summary>How much control the player has here: the owner's level, staff's, rights, or none.</summary>
    private int ControllerLevel(PlayerData player) =>
        _room!.OwnerId == player.Id ? _room.OwnerControllerLevel
        : permissions.Has(player, PermissionNames.RoomManageAny) ? RoomControllerLevels.Staff
        : GroupRankOf(player.Id) is GroupRank.Owner or GroupRank.Admin ? RoomControllerLevels.GroupAdmin
        : _groupRanks.ContainsKey(player.Id) && _group is { MembersCanDecorate: true } ? RoomControllerLevels.GroupRights
        : _rights.Contains(player.Id) ? RoomControllerLevels.Rights
        : RoomControllerLevels.None;

    /// <summary>Whether the player, here in the room, may handle its furniture.</summary>
    private bool IsController(Guid playerId) =>
        _entities?.Find(playerId) is { ControllerLevel: >= RoomControllerLevels.Rights };

    private bool IsOwnerOrStaff(Guid playerId) =>
        _room?.OwnerId == playerId || _entities?.Find(playerId) is { ControllerLevel: RoomControllerLevels.Staff };

    public async Task<FlatControllerAdded?> AssignRightsAsync(Guid ownerId, int accountId)
    {
        if (_room is null || !IsOwnerOrStaff(ownerId))
            return null;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId) is not { } user
            || user.Id == _room.OwnerId || !_rights.Add(user.Id))
            return null;

        db.RoomRights.Add(new RoomRight { RoomId = _room.Id, UserId = user.Id });
        await db.SaveChangesAsync();
        RefreshControl(user.Id);
        return new FlatControllerAdded(_room.Id, new FlatController(user.AccountId, user.Username));
    }

    public async Task<RightsRemoved> RemoveRightsAsync(Guid ownerId, IReadOnlyList<int> accountIds)
    {
        if (_room is null || !IsOwnerOrStaff(ownerId))
            return new RightsRemoved(_room?.Id ?? 0, []);

        await using var db = await dbFactory.CreateDbContextAsync();
        var users = await db.Users.AsNoTracking().Where(u => accountIds.Contains(u.AccountId))
            .Select(u => new { u.Id, u.AccountId }).ToListAsync();
        return await TakeRightsAsync(db, users.Where(u => _rights.Contains(u.Id)).Select(u => (u.Id, u.AccountId)).ToList());
    }

    public async Task<RightsRemoved> RemoveAllRightsAsync(Guid ownerId)
    {
        if (_room is null || !IsOwnerOrStaff(ownerId))
            return new RightsRemoved(_room?.Id ?? 0, []);

        await using var db = await dbFactory.CreateDbContextAsync();
        var holders = _rights.ToList();
        var users = await db.Users.AsNoTracking().Where(u => holders.Contains(u.Id))
            .Select(u => new { u.Id, u.AccountId }).ToListAsync();
        return await TakeRightsAsync(db, users.Select(u => (u.Id, u.AccountId)).ToList());
    }

    public async Task RemoveOwnRightsAsync(Guid playerId)
    {
        if (_room is null || !_rights.Contains(playerId))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var accountId = await db.Users.Where(u => u.Id == playerId).Select(u => u.AccountId).FirstAsync();
        var removed = await TakeRightsAsync(db, [(playerId, accountId)]);

        // the owner, if here, sees them leave the list
        foreach (var id in removed.AccountIds)
            SendTo(_room.OwnerId, new FlatControllerRemovedComposer(_room.Id, id));
    }

    public async Task<IReadOnlyList<FlatController>?> GetFlatControllersAsync(Guid requesterId)
    {
        if (_room is null || (_room.OwnerId != requesterId && !IsOwnerOrStaff(requesterId)))
            return null;

        await using var db = await dbFactory.CreateDbContextAsync();
        var holders = _rights.ToList();
        return await db.Users.AsNoTracking().Where(u => holders.Contains(u.Id))
            .OrderBy(u => u.Username)
            .Select(u => new FlatController(u.AccountId, u.Username)).ToListAsync();
    }

    private async Task<RightsRemoved> TakeRightsAsync(AppDbContext db, IReadOnlyList<(Guid Id, int AccountId)> users)
    {
        var ids = users.Select(u => u.Id).ToList();
        await db.RoomRights.Where(r => r.RoomId == _room!.Id && ids.Contains(r.UserId)).ExecuteDeleteAsync();
        foreach (var id in ids)
        {
            _rights.Remove(id);
            RefreshControl(id);
        }

        return new RightsRemoved(_room!.Id, users.Select(u => u.AccountId).ToList());
    }

    /// <summary>Tells a user here what control they now have, and everyone their new status.</summary>
    private void RefreshControl(Guid playerId)
    {
        if (_entities?.Find(playerId) is not { } entity)
            return;

        entity.ControllerLevel = ControllerLevel(entity.Player);
        SendTo(playerId, entity.ControllerLevel > RoomControllerLevels.None
            ? new YouAreControllerMessageComposer(_room!.Id, entity.ControllerLevel)
            : new YouAreNotControllerMessageComposer(_room!.Id));
        SendTo(playerId, WiredPermissions(entity));
        Broadcast(UserUpdateMessageComposer.From([_entities.CurrentStatus(entity)]));
    }

    /// <summary>Sends the new border mask of every water area a change reshaped. The item the
    /// change was about already carries its state in the message that announced it.</summary>
    private void BroadcastWaterAreas(List<RoomItemStore.PlacedItem> reshaped, int announcedItemId)
    {
        foreach (var item in reshaped.Where(item => item.Item.Id != announcedItemId))
            Broadcast(new ObjectDataUpdateMessageComposer(item.Item.Id, FurniStuffData.From(item.Data)));
    }

    public async Task UseItemAsync(Guid playerId, int itemId, FurniAction action, int param)
    {
        if (_room is null || _furniture is null || _items is null || _entities?.Find(playerId) is not { } entity)
            return;

        if (!entity.UsedFurni)
        {
            entity.UsedFurni = true;
            GrainFactory.GetGrain<IPlayerGrain>(playerId).ReachAchievementAsync(AchievementNames.HabboExplorer, 1).Ignore();
        }

        // Room rights do not exist yet, so the owner is the only controller.
        var isController = entity.ControllerLevel >= RoomControllerLevels.Rights;

        // clocks belong to the room's games
        if (_games is not null && _items.Find(itemId) is { } clockItem && RoomGames.IsClock(clockItem))
        {
            if (isController)
                await _games.UseClockAsync(clockItem, param == 0 ? GameClockInteraction.StartStop : param);
            return;
        }

        // a Freeze tile or block, double-clicked, throws a snowball on the thrower's own tile
        if (_games is not null && _items.Find(itemId) is { } freezeItem && RoomGames.IsThrow(freezeItem))
        {
            await _games.ThrowAsync(entity);
            return;
        }

        // teleports and hoppers take whoever uses them somewhere else
        if (_items.Find(itemId) is { } teleport && IsTeleport(teleport))
        {
            await UseTeleportAsync(entity, teleport);
            return;
        }

        // using a wired box opens its dialog, for those who may change the room's wired
        if (_wired?.IsBox(itemId) == true)
        {
            if (WiredRights(entity).Read && await _wired.DialogForAsync(itemId) is { } dialog)
                SendTo(playerId, dialog);
            return;
        }

        // a box with a pet in it is opened by naming the pet, and a seed is planted
        if (_items.Find(itemId) is { } boxed && (await OfferPackagedPetAsync(entity, boxed) || await PlantSeedAsync(entity, boxed)))
            return;

        // friendship furniture is locked by two friends together
        if (_items.Find(itemId) is { } friendFurni && await StartFriendLockAsync(entity, friendFurni))
            return;

        // using a chest opens it, and a contract its dialog
        if (_items.Find(itemId) is { } chest && (OpenChestOnUse(entity, chest) || OpenContractOnUse(entity, chest)))
            return;

        var before = _items.Find(itemId)?.Data.State;
        await _furniture.UseAsync(entity, isController, itemId, action, param);

        if (_wired is not null && _items.Find(itemId) is { } used && used.Data.State != before)
            await _wired.FurniStateChangedAsync(entity, used);
    }

    public async Task OpenWiredAsync(Guid playerId, int itemId)
    {
        if (_entities?.Find(playerId) is { } entity && WiredRights(entity).Read && _wired is not null
            && await _wired.DialogForAsync(itemId) is { } dialog)
            SendTo(playerId, dialog);
    }

    public async Task SaveWiredAsync(Guid playerId, WiredSaveData save)
    {
        if (_entities?.Find(playerId) is not { } entity || !WiredRights(entity).Modify || _wired is null)
            return;

        if (await _wired.SaveAsync(save, permission => permissions.Has(entity.Player, permission)) is { } answer)
            SendTo(playerId, answer);
    }

    public Task ApplyWiredSnapshotAsync(Guid playerId, int itemId) =>
        _entities?.Find(playerId) is { } entity && WiredRights(entity).Modify && _wired is not null
            ? _wired.ApplySnapshotAsync(itemId)
            : Task.CompletedTask;

    public async Task ChangePostureAsync(Guid playerId, int posture)
    {
        if (_entities?.Find(playerId) is not { } entity)
            return;

        await WakeAsync(entity);
        if (_entities.ChangePosture(entity, posture) is { } update)
            Broadcast(UserUpdateMessageComposer.From([update]));

        await DrainEntityChangesAsync();
    }

    public async Task ExpressAsync(Guid playerId, int expression)
    {
        if (_entities?.Find(playerId) is not { } entity)
            return;

        switch ((AvatarExpression)expression)
        {
            case AvatarExpression.Idle:
                if (_entities.Sleep(entity))
                {
                    Broadcast(new SleepMessageComposer(entity.RoomIndex, Asleep: true));
                    await ActedAsync(entity, WiredUserActions.Sleep);
                }

                return;
            case AvatarExpression.Wave or AvatarExpression.Blow or AvatarExpression.Laugh
                or AvatarExpression.Cry or AvatarExpression.Jump or AvatarExpression.Respect:
                await WakeAsync(entity);
                Broadcast(new ExpressionMessageComposer(entity.RoomIndex, expression));
                if (WiredActionFor((AvatarExpression)expression) is { } action)
                    await ActedAsync(entity, action);
                return;
            // the club's: the expression, then the line the client draws for it (Habbo sends both, in that order)
            case AvatarExpression.Expression67 when entity.Player.ClubLevel != Habbo.Domain.Users.ClubLevel.None:
                await WakeAsync(entity);
                Broadcast(new ExpressionMessageComposer(entity.RoomIndex, expression));
                Broadcast(new SpecialSystemChatMessageComposer(entity.RoomIndex, expression));
                return;
        }
    }

    /// <summary>The wired action an expression is, when wired can see it.</summary>
    private static int? WiredActionFor(AvatarExpression expression) => expression switch
    {
        AvatarExpression.Wave => WiredUserActions.Wave,
        AvatarExpression.Blow => WiredUserActions.Blow,
        AvatarExpression.Laugh => WiredUserActions.Laugh,
        _ => null,
    };

    public async Task DanceAsync(Guid playerId, int style)
    {
        if (_entities?.Find(playerId) is not { } entity
            || style is < AvatarActions.NoDance or > AvatarActions.LastDance
            || (style > AvatarActions.FreeDance && entity.Player.ClubLevel == ClubLevel.None))
            return;

        await WakeAsync(entity);
        if (!_entities.Dance(entity, style))
            return;

        Broadcast(new DanceMessageComposer(entity.RoomIndex, style));
        if (style != AvatarActions.NoDance)
            await ActedAsync(entity, WiredUserActions.Dance, WiredUserActions.DanceExtra(style));
    }

    public async Task ShowSignAsync(Guid playerId, int sign)
    {
        if (_entities?.Find(playerId) is not { } entity || sign is < AvatarActions.FirstSign or > AvatarActions.LastSign)
            return;

        await WakeAsync(entity);
        // the next tick tells it
        _entities.ShowSign(entity, sign);
        await ActedAsync(entity, WiredUserActions.Sign, WiredUserActions.SignExtra(sign));
    }

    public async Task RespectAsync(Guid playerId, int targetAccountId)
    {
        if (_entities?.Find(playerId) is not { } entity
            || _entities.Entities.FirstOrDefault(other => other.Player.AccountId == targetAccountId) is not { } target
            || target == entity
            || !await GrainFactory.GetGrain<IPlayerGrain>(playerId).SpendRespectAsync())
            return;

        var total = await GrainFactory.GetGrain<IPlayerGrain>(target.Player.Id).ReceiveRespectAsync();
        GrainFactory.GetGrain<IPlayerGrain>(playerId).ProgressAchievementAsync(AchievementNames.RespectGiven, 1).Ignore();
        GrainFactory.GetGrain<IPlayerGrain>(target.Player.Id).ProgressAchievementAsync(AchievementNames.RespectEarned, 1).Ignore();
        await WakeAsync(entity);
        Broadcast(new RespectNotificationMessageComposer(targetAccountId, total));
        Broadcast(new ExpressionMessageComposer(entity.RoomIndex, (int)AvatarExpression.Respect));
        await ActedAsync(entity, WiredUserActions.Respect);
    }

    /// <summary>Lifts or lowers a floor item to an altitude, as wired does, and shows everyone.</summary>
    private async Task<bool> SetPlacedAltitudeAsync(RoomItemStore.PlacedItem placed, double altitude)
    {
        if (_items is null || placed.Item.Definition!.Type != ItemType.Floor || altitude < 0 || altitude > RoomItemStore.MaxAltitude)
            return false;

        placed.Z = Math.Round(altitude, 2);
        _wired?.Moved();

        await using var db = await dbFactory.CreateDbContextAsync();
        // wired's temporary copies have no row
        if (!placed.Temporary && await db.UserItems.FirstOrDefaultAsync(i => i.Id == placed.Item.Id) is { } row)
        {
            row.Z = placed.Z;
            await db.SaveChangesAsync();
        }

        Broadcast(new ObjectUpdateMessageComposer(FloorItemData.From(RoomItemStore.ToWireData(placed), withOwnerName: false)));
        BroadcastHeights(RoomItemStore.Footprint(placed));
        return true;
    }

    /// <summary>Sends the user out of the room, to the hotel view, without saying why: they left by the door, or stood
    /// idle for longer than the room lets them.</summary>
    private async Task SendOutAsync(RoomEntityStore.RoomEntity entity)
    {
        SendTo(entity.Player.Id, new CloseConnectionMessageComposer());
        await LeaveAsync(entity.Player.Id);
    }

    /// <summary>Sends the user out of the room: the client leaves it, told it was kicked.</summary>
    private async Task KickAsync(RoomEntityStore.RoomEntity entity)
    {
        SendTo(entity.Player.Id, new GenericErrorComposer(GenericErrorCodes.Kicked));
        SendTo(entity.Player.Id, new CloseConnectionMessageComposer());
        await LeaveAsync(entity.Player.Id);
    }

    /// <summary>Holds a Freeze player where they stand, or lets them go: frozen, a walk under way is dropped.</summary>
    private static void SetGameFrozen(RoomEntityStore.RoomEntity entity, bool frozen)
    {
        entity.Frozen = frozen;
        if (!frozen)
            return;
        entity.Path.Clear();
        entity.Goal = null;
    }

    /// <summary>Dresses the avatar in an effect (0 takes it off), for everyone to see.</summary>
    private void ApplyEffect(RoomEntityStore.RoomEntity entity, int effectId)
    {
        if (entity.EffectId == effectId)
            return;

        entity.EffectId = effectId;
        Broadcast(new AvatarEffectMessageComposer(entity.RoomIndex, effectId, DelayMilliseconds: 0));
    }

    /// <summary>The avatar did something: if it was asleep, it wakes, and the room sees.</summary>
    private async Task WakeAsync(RoomEntityStore.RoomEntity entity)
    {
        if (_entities is null || !_entities.Wake(entity))
            return;

        Broadcast(new SleepMessageComposer(entity.RoomIndex, Asleep: false));
        await ActedAsync(entity, WiredUserActions.Awake);
    }

    /// <summary>Notes what the avatar did, and tells the room's wired.</summary>
    private async Task ActedAsync(RoomEntityStore.RoomEntity entity, int action, string extra = "")
    {
        _entities?.RecordAction(entity, action, extra);
        if (_wired is not null)
            await _wired.UserActedAsync(entity, action, extra);
    }

    /// <summary>Passes on what happened to avatars as they moved: postures to wired, a dance a seat
    /// stopped and an idle avatar falling asleep to the room.</summary>
    private async Task DrainEntityChangesAsync()
    {
        if (_entities is null)
            return;

        foreach (var (entity, change) in _entities.TakeChanges())
        {
            // what a bot or a pet does is not a user's action: wired hears none of it
            if ((entity.Bot is not null || entity.Pet is not null) && change is RoomEntityStore.EntityChange.SatDown or RoomEntityStore.EntityChange.LayDown
                    or RoomEntityStore.EntityChange.StoodUp)
                continue;

            switch (change)
            {
                case RoomEntityStore.EntityChange.SatDown:
                    await ActedAsync(entity, WiredUserActions.Sit);
                    break;
                case RoomEntityStore.EntityChange.LayDown:
                    await ActedAsync(entity, WiredUserActions.Lay);
                    break;
                case RoomEntityStore.EntityChange.StoodUp:
                    await ActedAsync(entity, WiredUserActions.Stand);
                    break;
                case RoomEntityStore.EntityChange.DanceStopped:
                    Broadcast(new DanceMessageComposer(entity.RoomIndex, AvatarActions.NoDance));
                    break;
                case RoomEntityStore.EntityChange.FellAsleep:
                    Broadcast(new SleepMessageComposer(entity.RoomIndex, Asleep: true));
                    await ActedAsync(entity, WiredUserActions.Sleep);
                    break;
                case RoomEntityStore.EntityChange.IdledOut:
                    await SendOutAsync(entity);
                    break;
                case RoomEntityStore.EntityChange.Arrived:
                    if (entity.OnArrival is { } then)
                    {
                        entity.OnArrival = null;
                        await then();
                    }
                    else if (_room!.LeaveOnDoorTile && entity.Bot is null && entity.Pet is null
                             && entity.X == _model!.DoorX && entity.Y == _model.DoorY)
                    {
                        // walking back out of the door leaves the room
                        await SendOutAsync(entity);
                    }

                    if (entity.Bot is not null && _wired is not null)
                        await _wired.BotArrivedAsync(entity);
                    break;
            }
        }
    }

    private async Task ShortTickAsync()
    {
        if (_scheduled.Count > 0)
            await RunScheduledAsync();

        if (_wired is { HasShortTicks: true } && _entities is { Count: > 0 })
            await _wired.ShortTickAsync();
    }

    public async Task ClickFurniAsync(Guid playerId, int itemId)
    {
        if (_entities?.Find(playerId) is { } entity && _items?.Find(itemId) is { } item && _wired is not null)
            await _wired.FurniClickedAsync(entity, item);
    }

    public async Task ClickUserAsync(Guid playerId, int roomIndex)
    {
        if (_entities?.Find(playerId) is not { } clicker || _entities.FindByIndex(roomIndex) is not { } clicked || _wired is null)
            return;

        await WakeAsync(clicker);
        var (openMenu, turn) = await _wired.UserClickedAsync(clicker, clicked);
        if (turn && _entities.LookTo(clicker, clicked.X, clicked.Y) is { } update)
            Broadcast(UserUpdateMessageComposer.From([update]));
        SendTo(playerId, new WiredClickUserResponseComposer(roomIndex, openMenu));
    }

    public async Task LookToAsync(Guid playerId, int x, int y)
    {
        if (_entities?.Find(playerId) is not { } entity)
            return;

        await WakeAsync(entity);
        if (_entities.LookTo(entity, x, y) is { } update)
            Broadcast(UserUpdateMessageComposer.From([update]));
    }

    private async Task TickAsync()
    {
        var started = DateTime.UtcNow;
        try
        {
            await TickCoreAsync();
        }
        finally
        {
            ArmNextTick(started);
        }
    }

    /// <summary>
    /// Arms the next tick a whole interval after this one started. A periodic grain timer counts its period from the
    /// end of each tick, so ticks came the tick's own running time late, every time: a client animates a step over
    /// exactly one interval, so everyone walking stood still for that long between steps. A room that falls behind
    /// (a tick longer than the interval) starts again from now rather than rushing ticks out to catch up.
    /// </summary>
    private void ArmNextTick(DateTime started)
    {
        _nextTickAt = (_nextTickAt == default ? started : _nextTickAt) + TickInterval;
        var now = DateTime.UtcNow;
        if (_nextTickAt < now)
            _nextTickAt = now;
        _tickTimer?.Change(_nextTickAt - now, Timeout.InfiniteTimeSpan);
    }

    private async Task TickCoreAsync()
    {
        // A timer is not a grain call, so it never resets Orleans' idle collection: a room where
        // nobody moves would otherwise be collected out from under the people standing in it.
        if (_entities is { Count: > 0 })
            DelayDeactivation(OccupiedRoomLifetime);

        if (_entities is null)
            return;

        ExpireEvent();

        var before = _entities.Entities.Select(entity => (entity, entity.X, entity.Y)).ToList();
        var updates = _entities.Tick();
        if (updates is { Count: > 0 })
            Broadcast(UserUpdateMessageComposer.From(updates));

        await DrainEntityChangesAsync();
        await ExpireRentalsAsync();
        await ExpireResolutionsAsync();
        await BotsActAsync();
        await PetsActAsync();
        await RollAsync();

        if (_wired is null)
            return;

        foreach (var (entity, fromX, fromY) in before)
        {
            // whoever left meanwhile (out of the door, idled out) steps on nothing more here
            if ((entity.X == fromX && entity.Y == fromY) || _entities.Find(entity.Player.Id) != entity)
                continue;

            if (entity.Bot is null && entity.Pet is null && ++entity.TilesWalkedUncounted >= TilesCountedTogether)
                CountTilesWalked(entity);

            if (_games is not null && _items is not null)
            {
                _games.UserSteppedOff(entity, fromX, fromY);
                // the walk ends on this tile: a football here is kicked rather than dribbled
                var arriving = entity.Path.Count == 0;
                foreach (var item in _items.FloorItemsOn(entity.X, entity.Y).ToList())
                {
                    await _games.UserSteppedOnAsync(entity, item, fromX, fromY, arriving);
                    await item.Interaction.WalkedOnAsync(new FurniWalk(item, entity, _furniture!));
                }
            }

            await _wired.UserSteppedAsync(entity, fromX, fromY);
        }

        if (_games is not null)
            await _games.PulseAsync();
        await _wired.PulseAsync();
    }

    /// <summary>Where the client points its camera when the room opens. Habbo sends the door
    /// tile, so a room that has never had a camera set gets the same rather than the corner.</summary>
    private static RoomVisualizationData Visualization(Room room, RoomModel model)
    {
        var hasCamera = room.CameraInitX != 0 || room.CameraInitY != 0 || room.CameraInitZ != 0;
        return new RoomVisualizationData(
            room.FloorType,
            room.Wallpaper,
            room.Landscape,
            room.WallsHidden,
            room.WallThicknessExponent,
            room.FloorThicknessExponent,
            room.UseSmallScale,
            room.FixedWallsHeight,
            hasCamera ? room.CameraInitX : model.DoorX,
            hasCamera ? room.CameraInitY : model.DoorY,
            hasCamera ? room.CameraInitZ : model.DoorHeight);
    }

    private static Task<UserItem?> LoadUnplacedItemAsync(AppDbContext db, int itemId, Guid ownerId, ItemType type) =>
        WithDefinitions(db.UserItems)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.OwnerId == ownerId && i.RoomId == null && !i.InMarketplace && i.ChestId == null
                                      && i.Definition!.Type == type);

    /// <summary>Items as the room needs them: with the owner for the wire, and the definition's
    /// states and rotations for behaviours and placement.</summary>
    private static IQueryable<UserItem> WithDefinitions(IQueryable<UserItem> items) =>
        items
            .Include(i => i.Definition).ThenInclude(d => d!.States)
            .Include(i => i.Definition).ThenInclude(d => d!.Directions)
            .Include(i => i.Owner)
            .AsSplitQuery();

    /// <summary>Runs <paramref name="action"/> once after <paramref name="delay"/>, on the grain's turn.</summary>
    /// <summary>Runs <paramref name="action"/> once after <paramref name="delay"/>, on the grain's turn. The
    /// short tick runs it: a grain timer registered from inside the room's own tick never fires, and
    /// much of what the room schedules starts there (a pet arriving at its food).</summary>
    private void Schedule(TimeSpan delay, Func<Task> action) => _scheduled.Add((DateTime.UtcNow + delay, action));

    /// <summary>What is waiting to run, and when.</summary>
    private readonly List<(DateTime Due, Func<Task> Action)> _scheduled = [];

    /// <summary>Runs whatever is due, in the order it fell due.</summary>
    private async Task RunScheduledAsync()
    {
        var now = DateTime.UtcNow;
        var due = _scheduled.Where(entry => entry.Due <= now).OrderBy(entry => entry.Due).ToList();
        foreach (var entry in due)
        {
            _scheduled.Remove(entry);
            await entry.Action();
        }
    }

    private void Broadcast(IComposer composer)
    {
        if (_entities is null)
            return;

        var message = composer.ToMessage();
        foreach (var entity in _entities.Entities)
            sink.TrySend(entity.SessionGuid, message);
        foreach (var spectator in SpectatorSessions)
            sink.TrySend(spectator, message);
    }

    /// <summary>A chat line, a copy for each listener, as Habbo writes it: each copy names whom it is for, and only
    /// the speaker's carries the tracking id their client sent. Spectators, with no avatar, get one for nobody.</summary>
    private void SendChat(ChatMessage message, RoomChatKind kind, IEnumerable<RoomEntityStore.RoomEntity> listeners, bool toSpectators)
    {
        foreach (var listener in listeners)
            sink.TrySend(listener.SessionGuid,
                ChatComposer(message, kind, listener.RoomIndex, toSpeaker: listener.RoomIndex == message.RoomIndex).ToMessage());

        if (!toSpectators)
            return;
        var forSpectators = ChatComposer(message, kind, ChatMessageComposer.NoReceiver, toSpeaker: false).ToMessage();
        foreach (var spectator in SpectatorSessions)
            sink.TrySend(spectator, forSpectators);
    }

    private static IComposer ChatComposer(ChatMessage message, RoomChatKind kind, int receiverRoomIndex, bool toSpeaker) => kind switch
    {
        RoomChatKind.Whisper => WhisperMessageComposer.From(message, receiverRoomIndex, toSpeaker),
        RoomChatKind.Shout => ShoutMessageComposer.From(message, receiverRoomIndex, toSpeaker),
        _ => ChatMessageComposer.From(message, receiverRoomIndex, toSpeaker),
    };

    /// <summary>Those within the room's hearing distance of the speaker, in tiles either way.</summary>
    private IEnumerable<RoomEntityStore.RoomEntity> WithinHearing(RoomEntityStore.RoomEntity speaker)
    {
        var range = _room!.ChatHearRange;
        return _entities!.Entities.Where(e => Math.Max(Math.Abs(e.X - speaker.X), Math.Abs(e.Y - speaker.Y)) <= range).ToList();
    }

    private void BroadcastExcept(Guid excludedPlayerId, IComposer composer)
    {
        if (_entities is null)
            return;

        var message = composer.ToMessage();
        foreach (var entity in _entities.Entities)
        {
            if (entity.Player.Id != excludedPlayerId)
                sink.TrySend(entity.SessionGuid, message);
        }

        foreach (var spectator in SpectatorSessions)
            sink.TrySend(spectator, message);
    }

    private void SendTo(Guid playerId, IComposer composer)
    {
        if (_entities?.Find(playerId) is { } entity)
            sink.TrySend(entity.SessionGuid, composer.ToMessage());
    }
}
