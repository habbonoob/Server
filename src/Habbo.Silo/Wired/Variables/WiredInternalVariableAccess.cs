using System.Globalization;
using System.Numerics;
using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Users;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Chests;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Variables;

/// <summary>What this server reads and writes of habbo.com's internal and smart variables, by holder and Habbo's name.
/// Every one of Habbo's is listed whether or not it is here; one with no entry is held by no one (its reader is null)
/// and is not written. A variable with no value (hasValue false: a flag) reads 0 for a holder that has it, null for
/// one that does not.</summary>
public static class WiredInternalVariableAccess
{
    private const double Hundredths = 100.0;

    /// <summary>The room's own teams, in Habbo's names.</summary>
    private static readonly (string Name, int Team)[] Teams =
        [("red", RoomGames.Red), ("green", RoomGames.Green), ("blue", RoomGames.Blue), ("yellow", RoomGames.Yellow)];

    /// <summary>What @type says of a furni (wired's own temporary copies, Builders Club furni, any other) and a user.</summary>
    private const int NormalFurni = 0;
    private const int BuildersClubFurni = 1;
    private const int TemporaryFurni = 2;
    private const int UnknownGender = -1;

    /// <summary>What ~clock.state says of a clock.</summary>
    private const int ClockInitial = 0;
    private const int ClockRunning = 1;
    private const int ClockPaused = 2;

    /// <summary>What @transaction.state says of a wired trade.</summary>
    private const int AddingItems = 1;
    private const int Confirmed = 2;

    /// <summary>The logic of furni the client lets be recoloured; their colour is three numbers in their map data.</summary>
    public const string RecolorableLogic = "furniture_recolorable";
    public const string RedKey = "r";
    public const string GreenKey = "g";
    public const string BlueKey = "b";
    private const string StateKey = "state";

    /// <summary>Every holder and name this server has an entry for: each must be one of Habbo's.</summary>
    public static IEnumerable<(int Target, string Name)> Names => Table.Keys;

    public static WiredInternalAccess For(int target, string name) =>
        Table.GetValueOrDefault((target, name)) ?? WiredInternalAccess.None;

    private static readonly Dictionary<(int Target, string Name), WiredInternalAccess> Table = Build();

    private static Dictionary<(int, string), WiredInternalAccess> Build()
    {
        var table = new Dictionary<(int, string), WiredInternalAccess>();

        void Furni(string name, Func<RoomItemStore.PlacedItem, IWiredRoom, int?> read,
            Func<RoomItemStore.PlacedItem, int, IWiredRoom, Task<bool>>? write = null,
            Func<RoomItemStore.PlacedItem, bool, IWiredRoom, Task<bool>>? giveOrRemove = null) =>
            table.Add((WiredVariableTargets.Furni, name), new WiredInternalAccess(
                (holder, _, room) => holder.Item is { } item ? read(item, room) : null,
                write is null ? null : (holder, value, room) => holder.Item is { } item ? write(item, value, room) : False,
                giveOrRemove is null ? null : (holder, _, room) => holder.Item is { } item ? giveOrRemove(item, true, room) : False,
                giveOrRemove is null ? null : (holder, room) => holder.Item is { } item ? giveOrRemove(item, false, room) : False));

        void User(string name, Func<RoomEntityStore.RoomEntity, IWiredRoom, int?> read,
            Func<RoomEntityStore.RoomEntity, int, IWiredRoom, Task<bool>>? write = null,
            Func<RoomEntityStore.RoomEntity, int, IWiredRoom, Task<bool>>? give = null,
            Func<RoomEntityStore.RoomEntity, IWiredRoom, Task<bool>>? remove = null) =>
            table.Add((WiredVariableTargets.User, name), new WiredInternalAccess(
                (holder, _, room) => holder.User is { } user ? read(user, room) : null,
                write is null ? null : (holder, value, room) => holder.User is { } user ? write(user, value, room) : False,
                give is null ? null : (holder, value, room) => holder.User is { } user ? give(user, value, room) : False,
                remove is null ? null : (holder, room) => holder.User is { } user ? remove(user, room) : False));

        void Global(string name, Func<IWiredRoom, int?> read, Func<int, IWiredRoom, Task<bool>>? write = null) =>
            table.Add((WiredVariableTargets.Global, name), new WiredInternalAccess(
                (_, _, room) => read(room),
                write is null ? null : (_, value, room) => write(value, room)));

        void Context(string name, Func<WiredRun, int?> read) =>
            table.Add((WiredVariableTargets.Context, name), new WiredInternalAccess((_, run, _) => run is null ? null : read(run)));

        // ---------- furni ----------

        Furni("@id", (item, _) => item.Id);
        Furni("@class_id", (item, _) => item.Item.Definition!.ClassId);
        Furni("@height", (item, _) => Centi(item.StackHeight ?? item.Item.Definition!.StackHeight));
        Furni("@state", (item, _) => int.TryParse(item.Data.StateText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var state) ? state : 0,
            async (item, value, room) =>
            {
                await room.SetStateAsync(item, value.ToString(CultureInfo.InvariantCulture));
                return true;
            });
        Furni("@position.x", (item, _) => item.X, (item, value, room) => MoveTo(item, value, item.Y, item.Rotation, room));
        Furni("@position.y", (item, _) => item.Y, (item, value, room) => MoveTo(item, item.X, value, item.Rotation, room));
        Furni("@rotation", (item, _) => item.Rotation, (item, value, room) => MoveTo(item, item.X, item.Y, value, room));
        Furni("@altitude", (item, _) => Centi(item.Z), (item, value, room) => room.SetAltitudeAsync(item, value / Hundredths));
        // packed as the client spells them out: x in the high byte and y in the low one; and x, y, rotation a byte each
        Furni("@position", (item, _) => PackPosition(item.X, item.Y),
            (item, value, room) => MoveTo(item, value >> 8 & 0xFF, value & 0xFF, item.Rotation, room));
        Furni("@occupation", (item, _) => item.X << 16 | (item.Y & 0xFF) << 8 | (item.Rotation & 0xFF),
            (item, value, room) => MoveTo(item, value >> 16 & 0xFF, value >> 8 & 0xFF, value & 0xFF, room));
        Furni("@type", (item, _) => item.Temporary ? TemporaryFurni : item.Item.BuildersClub ? BuildersClubFurni : NormalFurni);
        Furni("@is_stackable", (item, _) => Flag(item.Item.Definition!.CanStackOn));
        Furni("@can_stand_on", (item, _) => Flag(item.Item.Definition!.CanWalk));
        Furni("@can_sit_on", (item, _) => Flag(item.Item.Definition!.CanSit));
        Furni("@can_lay_on", (item, _) => Flag(item.Item.Definition!.CanLay));
        Furni("@dimensions.x", (item, _) => item.Item.Definition!.XDim);
        Furni("@dimensions.y", (item, _) => item.Item.Definition!.YDim);
        Furni("@owner_id", (item, _) => item.Item.Owner?.AccountId ?? 0);
        Furni("@is_invisible", (item, _) => Flag(item.Invisible));
        // a wall item's offset on its wall tile, in pixels: x in the high 16 bits, y in the low, each signed
        Furni("@wallitem_offset", (item, _) => WallLocation.TryParse(item.Item.WallPosition, out var at) ? at.OffsetX << 16 | (at.OffsetY & 0xFFFF) : null,
            (item, value, room) => WallLocation.TryParse(item.Item.WallPosition, out var at)
                ? room.MoveWallItemAsync(item, at with { OffsetX = value >> 16, OffsetY = (short)(value & 0xFFFF) })
                : False);
        Furni("@can_move_freely", (item, _) => Flag(item.MovesFreely), giveOrRemove: (item, on, _) =>
        {
            item.MovesFreely = on;
            return True;
        });

        // a furni's last flight as a wired projectile
        Furni("@projectile.animation.tiles_travelled", (item, _) => item.Projectile?.Tiles);
        Furni("@projectile.animation.user_collisions", (item, _) => item.Projectile?.UserCollisions);
        Furni("@projectile.animation.furni_collisions", (item, _) => item.Projectile?.FurniCollisions);
        Furni("@projectile.animation.position.x", (item, room) => item.Projectile?.PositionAt(room.Now()).X);
        Furni("@projectile.animation.position.y", (item, room) => item.Projectile?.PositionAt(room.Now()).Y);
        Furni("@projectile.animation.position.altitude", (item, room) => item.Projectile is { } flight ? Centi(flight.PositionAt(room.Now()).Z) : null);
        Furni("@projectile.animation.is_travelling", (item, room) => item.Projectile is { } flight ? Flag(flight.IsTravelling(room.Now())) : null);

        // an area hider's numbers
        AreaHideNumber("~area_hide.root_x", AreaHideInteraction.RootXIndex);
        AreaHideNumber("~area_hide.root_y", AreaHideInteraction.RootYIndex);
        AreaHideNumber("~area_hide.width", AreaHideInteraction.WidthIndex);
        AreaHideNumber("~area_hide.length", AreaHideInteraction.LengthIndex);
        AreaHideFlag("~area_hide.is_invisible_furni", AreaHideInteraction.InvisibilityIndex);
        AreaHideFlag("~area_hide.hiding_wallitems", AreaHideInteraction.WallItemsIndex);
        AreaHideFlag("~area_hide.inverted", AreaHideInteraction.InvertIndex);

        void AreaHideNumber(string name, int index) => Furni(name,
            (item, _) => item.Interaction is AreaHideInteraction ? AreaHideInteraction.Values(item.Data)[index] : null,
            (item, value, room) => SetAreaHide(item, index, value, room));

        void AreaHideFlag(string name, int index) => Furni(name,
            (item, _) => item.Interaction is AreaHideInteraction ? Flag(AreaHideInteraction.Values(item.Data)[index] == AreaHideInteraction.True) : null,
            giveOrRemove: (item, on, room) => SetAreaHide(item, index, on ? AreaHideInteraction.True : AreaHideInteraction.False, room));

        // a background toner's colour
        Toner("~background_color.hue", BackgroundColorInteraction.HueIndex);
        Toner("~background_color.saturation", BackgroundColorInteraction.SaturationIndex);
        Toner("~background_color.lightness", BackgroundColorInteraction.LightnessIndex);

        void Toner(string name, int index) => Furni(name,
            (item, _) => item.Interaction is BackgroundColorInteraction ? BackgroundColorInteraction.Values(item.Data)[index] : null,
            async (item, value, room) =>
            {
                if (item.Interaction is not BackgroundColorInteraction)
                    return false;
                var values = BackgroundColorInteraction.Values(item.Data);
                values[index] = Math.Clamp(value, 0, byte.MaxValue);
                return await room.UpdateFurniDataAsync(item, BackgroundColorInteraction.WithValues(item.Data, values));
            });

        // a chest, as its settings stand
        Chest("~chest.available_amount", chest => chest.ContentsCount);
        Chest("~chest.capacity", chest => chest.Capacity);
        Chest("~chest.locked", chest => Flag(chest.Locked));
        Chest("~chest.is_auto_lock", chest => Flag(chest.AutoLock));
        Chest("~chest.is_open", chest => Flag(chest.State != ChestSettings.ClosedState));
        Chest("~chest.is_donatable", chest => Flag(chest.EveryoneCanDonate));

        void Chest(string name, Func<ChestSettings, int?> read) =>
            Furni(name, (item, _) => item.Interaction is ChestInteraction ? read(ChestSettings.Read(item.Data)) : null);

        // a game clock
        Furni("~clock.state", (item, room) => room.Games.Clock(item) is { } clock
            ? clock.Running ? ClockRunning : clock.Fresh ? ClockInitial : ClockPaused
            : null);
        // every clock of this server's is a game's: starting one fresh starts the game, and stopping it ends it
        Furni("~clock.is_game_aware", (item, room) => room.Games.Clock(item) is not null ? Flag(true) : null);
        Furni("~clock.pulse_count", (item, room) => room.Games.Clock(item)?.Pulses, async (item, value, room) =>
        {
            if (room.Games.Clock(item) is not { } clock)
                return false;
            await room.Games.SetTimeAsync(clock, value);
            return true;
        });

        // a recolourable furni's colour, as the client draws it: each channel 255 until it is set
        Furni("~recolorable_furni.color.rgb", (item, _) => ColorOf(item) is { } c ? c.R << 16 | c.G << 8 | c.B : null,
            (item, value, room) => ColorOf(item) is null
                ? False
                : room.UpdateFurniDataAsync(item, WithColor(item.Data, value >> 16 & 0xFF, value >> 8 & 0xFF, value & 0xFF)));
        Furni("~recolorable_furni.color.rgb.red", (item, _) => ColorOf(item)?.R);
        Furni("~recolorable_furni.color.rgb.green", (item, _) => ColorOf(item)?.G);
        Furni("~recolorable_furni.color.rgb.blue", (item, _) => ColorOf(item)?.B);

        // a teleport's partner, or the teleport wired pointed it at
        Furni("~teleport.target_id", (item, room) => room.TeleportTarget(item), (item, value, room) => room.SetTeleportTargetAsync(item, value));

        Furni("~walk_helper.is_multi_walk", (item, _) => item.Interaction is StackHelperInteraction ? Flag(item.MultiWalk) : null);

        // ---------- users ----------

        User("@index", (user, _) => user.RoomIndex);
        User("@type", (user, _) => user.Pet is not null ? WiredUserKinds.Pet : user.Bot is not null ? WiredUserKinds.Bot : WiredUserKinds.Habbo);
        User("@gender", (user, _) => user.Pet is not null ? UnknownGender
            : (user.Bot?.Data.Gender ?? user.Player.Gender) == Gender.Female ? 1 : 0);
        // a pet's level; everyone else is at none
        User("@level", (user, _) => user.Pet?.Data.Level ?? 0);
        User("@achievement_score", (user, _) => IsHabbo(user) ? user.Player.AchievementScore : 0);
        User("@is_hc", (user, _) => Flag(IsHabbo(user) && user.Player.ClubLevel != ClubLevel.None));
        User("@has_rights", (user, _) => Flag(user.ControllerLevel >= RoomControllerLevels.Rights));
        User("@is_group_admin", (user, room) => Flag(room.GroupId is not null && user.ControllerLevel >= RoomControllerLevels.GroupAdmin));
        User("@is_owner", (user, room) => Flag(room.IsOwner(user)));
        User("@position.x", (user, _) => user.X, (user, value, room) => Warp(user, value, user.Y, room));
        User("@position.y", (user, _) => user.Y, (user, value, room) => Warp(user, user.X, value, room));
        User("@direction", (user, _) => user.BodyDirection, (user, value, room) =>
        {
            room.TurnUser(user, ((value % 8) + 8) % 8);
            return True;
        });
        User("@altitude", (user, _) => Centi(user.Z));
        User("@position", (user, _) => PackPosition(user.X, user.Y), (user, value, room) => Warp(user, value >> 8 & 0xFF, value & 0xFF, room));
        User("@team.score", (user, room) => user.Team != RoomGames.NoTeam ? room.Games.Score(user.Team) : null,
            (user, value, room) => user.Team != RoomGames.NoTeam ? SetScore(room, user.Team, value) : False);
        User("@team.color", (user, _) => user.Team != RoomGames.NoTeam ? user.Team : null);
        User("@team.type", (user, _) => user.Team != RoomGames.NoTeam ? user.TeamGame : null);
        User("@handitem", (user, _) => user.HandItem != 0 ? user.HandItem : null,
            (user, value, room) => HandOver(user, value, room),
            give: (user, value, room) => value > 0 ? HandOver(user, value, room) : False,
            remove: (user, room) => user.HandItem != 0 ? HandOver(user, 0, room) : False);
        User("@effect", (user, _) => user.EffectId != 0 ? user.EffectId : null);
        User("@is_frozen", (user, _) => Flag(user.Frozen));
        User("@is_muted", (user, room) => Flag(user.MutedUntil > room.Now().UtcDateTime));
        User("@is_trading", (user, _) => Flag(user.Trading));
        // the Habbicon the user last showed: shown again by writing or giving one, put away by taking it
        table.Add((WiredVariableTargets.User, "@habbicon"), new WiredInternalAccess(
            (holder, _, _) => holder.User?.Habbicon,
            (holder, value, room) => holder.User is { } user ? room.ShowHabbiconAsync(user, value) : False,
            (holder, value, room) => holder.User is { } user ? room.ShowHabbiconAsync(user, value) : False,
            (holder, _) =>
            {
                if (holder.User is not { Habbicon: not null } user)
                    return False;
                user.Habbicon = null;
                return True;
            },
            (holder, _, _) => holder.User is { Habbicon: { } shown } user
                ? new WiredVariableValue(shown, user.HabbiconShownAt)
                : null));
        User("@favourite_group_id", (user, _) => user.FavouriteGroup?.Id);
        User("@dance", (user, _) => user.DanceStyle != 0 ? user.DanceStyle : null);
        User("@sign", (user, _) => user.Sign);
        User("@is_idle", (user, _) => Flag(user.Asleep));
        User("@user_id", (user, _) => IsHabbo(user) ? user.Player.AccountId : null);
        User("@pet_id", (user, _) => user.Pet?.Data.Id);
        User("@bot_id", (user, _) => user.Bot?.Data.Id);
        User("@pet_owner_id", (user, _) => user.Pet?.OwnerAccountId);
        User("@room_entry.method", (user, _) => user.EntryMethod);
        User("@room_entry.teleport_id", (user, _) => user.EntryTeleportId);

        // a wired trade: a deposit into a chest, or a payment or trade under a contract
        User("@transaction.in_trade", (user, room) => Flag(room.TradeOf(user) is not null));
        User("@transaction.contract_id", (user, room) => room.TradeOf(user)?.Terms?.ContractItemId);
        User("@transaction.state", (user, room) => room.TradeOf(user) is { } trade ? trade.Accepted ? Confirmed : AddingItems : null);
        User("@transaction.can_accept", (user, room) => Flag(room.TradeOf(user) is { } trade
            && (trade.Terms is null ? trade.Items.Count > 0 : trade.TimesMet > 0)));
        User("@transaction.current_multiplier", (user, room) => room.TradeOf(user)?.TimesMet);
        User("@transaction.start_time", (user, room) => room.TradeOf(user) is { } trade ? UnixSeconds(trade.StartedAt) : null);

        // a horse and its rider
        User("~horse.has_saddle", (user, _) => user.Pet is { } pet ? Flag(pet.Data.SaddleDefinitionId is not null) : null);
        User("~horse.is_riding", (user, _) => user.Pet is not null ? Flag(user.Rider is not null) : null);
        User("~horse.controller_user_id", (user, _) => user.Pet is not null ? user.Rider?.Player.AccountId : null);

        // a pet, as its info card shows it; a monster plant is a plant, not a pet
        Pet("~pet.level", pet => pet.Level);
        Pet("~pet.max_level", pet => pet.MaxLevel);
        Pet("~pet.experience", pet => pet.Experience);
        Pet("~pet.experience_required", pet => pet.ExperienceRequired);
        Pet("~pet.energy", pet => pet.Energy);
        Pet("~pet.max_energy", pet => pet.MaxEnergy);
        Pet("~pet.happiness", pet => pet.Happiness);
        Pet("~pet.max_happiness", pet => pet.MaxHappiness);
        Pet("~pet.scratches", pet => pet.Scratches);
        Pet("~pet.owner_id", pet => pet.OwnerAccountId);
        Pet("~pet.creation_time", pet => UnixSeconds(pet.CreatedAt));

        void Pet(string name, Func<WiredPetStats, int> read) =>
            User(name, (user, room) => user.Pet is not null && room.PetStats(user) is { Plant: null } pet ? read(pet) : null);

        Plant("~plant.rarity_level", (pet, _) => pet.Rarity);
        Plant("~plant.can_breed", (_, plant) => Flag(plant.CanBreed));
        Plant("~plant.can_harvest", (_, plant) => Flag(plant.CanHarvest));
        Plant("~plant.can_revive", (_, plant) => Flag(plant.CanRevive));
        Plant("~plant.is_dead", (_, plant) => Flag(plant.IsDead));
        Plant("~plant.is_growing", (_, plant) => Flag(plant.IsGrowing));
        Plant("~plant.max_wellbeing_seconds", (_, plant) => plant.MaxWellbeingSeconds);
        Plant("~plant.remaining_wellbeing_seconds", (_, plant) => plant.RemainingWellbeingSeconds);
        Plant("~plant.remaining_growing_seconds", (_, plant) => plant.RemainingGrowingSeconds);
        Plant("~plant.color", (_, plant) => plant.Color);
        Plant("~plant.shape", (_, plant) => plant.Shape >= 0 ? plant.Shape : null);

        void Plant(string name, Func<WiredPetStats, WiredPlantStats, int?> read) =>
            User(name, (user, room) => user.Pet is not null && room.PetStats(user) is { Plant: { } plant } pet ? read(pet, plant) : null);

        // ---------- the room ----------

        Global("@furni_count", room => room.Items.Items.Count());
        Global("@user_count", room => room.Entities.Count);
        Global("@wired_timer", room => room.TimerPulses);
        foreach (var (team, number) in Teams)
        {
            Global($"@teams.{team}.score", room => room.Games.Score(number), (value, room) => SetScore(room, number, value));
            Global($"@teams.{team}.size", room => room.Games.Members(number).Count());
        }

        Global("@room_id", room => room.RoomId);
        Global("@group_id", room => room.GroupId);

        // the time, in the room's time zone
        Global("@current_time", room => UnixSeconds(room.Now()));
        Global("@current_time.milliseconds_of_seconds", room => Local(room).Millisecond);
        Global("@current_time.seconds_of_minute", room => Local(room).Second);
        Global("@current_time.minute_of_hour", room => Local(room).Minute);
        Global("@current_time.hour_of_day", room => Local(room).Hour);
        Global("@current_time.day_of_week", room => Local(room).DayOfWeek is DayOfWeek.Sunday ? 7 : (int)Local(room).DayOfWeek);
        Global("@current_time.day_of_month", room => Local(room).Day);
        Global("@current_time.day_of_year", room => Local(room).DayOfYear);
        Global("@current_time.week_of_year", room => ISOWeek.GetWeekOfYear(Local(room).DateTime));
        Global("@current_time.month_of_year", room => Local(room).Month);
        Global("@current_time.year", room => Local(room).Year);

        // ---------- a stack's run ----------

        Context("@selector_furni_count", run => run.SelectedFurni.Count);
        Context("@selector_user_count", run => run.SelectedUsers.Count);
        Context("@signal_furni_count", run => run.Event.SignalFurni.Count);
        Context("@signal_user_count", run => run.Event.SignalUsers.Count);
        Context("@event.signal.antenna_id", run => run.Event.Type == WiredEventType.SignalReceived ? run.Event.Item?.Id : null);
        Context("@event.chat.type", run => run.Event.ChatType);
        Context("@event.chat.style", run => run.Event.ChatStyle);
        Context("@event.habbicon", run => run.Event.Habbicon);
        Context("@event.link.source_room_id", run => run.Event.LinkSourceRoomId);

        // a variable goes by its box's database id; the box, as @id has it, by the room's
        Context("@event.variable_update.box_id", run => VariableUpdate(run) is { } e
            && int.TryParse(e.VariableId?.Split(WiredSubVariables.Separator)[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var box)
                ? run.Room.Items.IdOfStored(box)
                : null);
        Context("@event.variable_update.change_type", run => VariableUpdate(run)?.VariableChange is { } change ? (int)change : null);
        Context("@event.variable_update.old_value", run => VariableUpdate(run)?.OldValue);
        Context("@event.variable_update.new_value", run => VariableUpdate(run)?.NewValue);
        Context("@event.variable_update.difference", run => VariableUpdate(run) is { } e ? (e.NewValue ?? 0) - (e.OldValue ?? 0) : null);
        // the origin is one bit of the trigger's mask: its place is what the text connector names
        Context("@event.variable_update.change_origin", run => VariableUpdate(run) is { VariableOrigin: > 0 } e
            ? BitOperations.TrailingZeroCount(e.VariableOrigin)
            : null);

        Context("@event.transaction_complete.multiplier", run => TransactionDone(run)?.Multiplier);
        Context("@event.transaction_complete.deposit.furni_count", run => TransactionDone(run)?.DepositedFurni);
        Context("@event.transaction_complete.deposit.coins_count", run => TransactionDone(run)?.DepositedCoins);
        Context("@event.transaction_complete.withdrawal.furni_count", run => TransactionDone(run)?.WithdrawnFurni);
        Context("@event.transaction_complete.withdrawal.coins_count", run => TransactionDone(run)?.WithdrawnCoins);
        Context("@event.transaction_failed.reason", run => run.Event.FailureReason);

        return table;
    }

    private static readonly Task<bool> True = Task.FromResult(true);
    private static readonly Task<bool> False = Task.FromResult(false);

    /// <summary>A flag's reading: held (0) or not (null).</summary>
    private static int? Flag(bool held) => held ? 0 : null;

    private static int Centi(double value) => (int)Math.Round(value * Hundredths);

    private static int UnixSeconds(DateTimeOffset time) => (int)Math.Clamp(time.ToUnixTimeSeconds(), int.MinValue, int.MaxValue);

    /// <summary>A recolourable furni's colour; null for any other furni.</summary>
    public static (int R, int G, int B)? ColorOf(RoomItemStore.PlacedItem item)
    {
        if (item.Item.Definition!.Logic != RecolorableLogic)
            return null;

        int Channel(string key) => item.Data.MapEntries?.FirstOrDefault(e => e.Key == key) is { } entry
            && int.TryParse(entry.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value <= byte.MaxValue
                ? value
                : byte.MaxValue;
        return (Channel(RedKey), Channel(GreenKey), Channel(BlueKey));
    }

    /// <summary>The furni's data with its colour set: its map data, keeping what else it holds (a plain state becomes the
    /// map's "state", which the client reads as the state).</summary>
    public static FurniDataView WithColor(FurniDataView data, int red, int green, int blue)
    {
        var entries = data.Type == (int)FurniDataType.Map
            ? (data.MapEntries ?? []).Where(e => e.Key is not (RedKey or GreenKey or BlueKey)).ToList()
            : string.IsNullOrEmpty(data.State) ? [] : [new FurniMapEntryView(StateKey, data.State)];
        entries.Add(new FurniMapEntryView(RedKey, red.ToString(CultureInfo.InvariantCulture)));
        entries.Add(new FurniMapEntryView(GreenKey, green.ToString(CultureInfo.InvariantCulture)));
        entries.Add(new FurniMapEntryView(BlueKey, blue.ToString(CultureInfo.InvariantCulture)));
        return data with { Type = (int)FurniDataType.Map, State = null, MapEntries = entries };
    }

    private static int PackPosition(int x, int y) => (x & 0xFF) << 8 | (y & 0xFF);

    private static bool IsHabbo(RoomEntityStore.RoomEntity user) => user is { Bot: null, Pet: null };

    private static DateTimeOffset Local(IWiredRoom room)
    {
        var now = room.Now();
        return room.Timezone.Length > 0 && TimeZoneInfo.TryFindSystemTimeZoneById(room.Timezone, out var zone)
            ? TimeZoneInfo.ConvertTime(now, zone)
            : now.ToUniversalTime();
    }

    private static WiredEvent? VariableUpdate(WiredRun run) => run.Event.Type == WiredEventType.VariableChanged ? run.Event : null;

    private static WiredTransactionDone? TransactionDone(WiredRun run) =>
        run.Event.Type == WiredEventType.TransactionCompleted ? run.Event.Transaction : null;

    private static Task<bool> MoveTo(RoomItemStore.PlacedItem item, int x, int y, int rotation, IWiredRoom room) =>
        room.MoveItemAsync(item, x, y, rotation, WiredMoveOptions.Default);

    private static Task<bool> Warp(RoomEntityStore.RoomEntity user, int x, int y, IWiredRoom room)
    {
        room.WarpUser(user, x, y);
        return True;
    }

    private static async Task<bool> HandOver(RoomEntityStore.RoomEntity user, int handItem, IWiredRoom room)
    {
        await room.HandOverAsync(null, user, Math.Max(0, handItem));
        return true;
    }

    private static async Task<bool> SetScore(IWiredRoom room, int team, int value)
    {
        await room.Games.AddScoreAsync(team, value - room.Games.Score(team));
        return true;
    }

    private static async Task<bool> SetAreaHide(RoomItemStore.PlacedItem item, int index, int value, IWiredRoom room)
    {
        if (item.Interaction is not AreaHideInteraction)
            return false;

        var values = AreaHideInteraction.Values(item.Data);
        values[index] = value;
        return await room.UpdateFurniDataAsync(item, AreaHideInteraction.WithValues(item.Data, values));
    }
}
