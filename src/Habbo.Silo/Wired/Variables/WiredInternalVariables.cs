using Habbo.Domain.Users;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Variables;

/// <summary>A variable the room keeps for everything of its kind, read from (and some written
/// to) what it describes: a furni's position, a user's effect, the room's user count.</summary>
public sealed class WiredInternalVariable(
    WiredVariable definition,
    Func<WiredHolder, WiredRun?, IWiredRoom, int?> read,
    Func<WiredHolder, int, IWiredRoom, Task<bool>>? write = null)
{
    public WiredVariable Definition { get; } = definition;

    /// <summary>The value for the holder; null when the holder does not have it.</summary>
    public int? Read(WiredHolder holder, WiredRun? run, IWiredRoom room) => read(holder, run, room);

    public Task<bool> WriteAsync(WiredHolder holder, int value, IWiredRoom room) =>
        write is null ? Task.FromResult(false) : write(holder, value, room);
}

/// <summary>The internal variables (the client's "internal" tab), named as Habbo names them.
/// Only what this server keeps is offered: a variable for a feature it does not have yet would
/// be a lie about the room.</summary>
public static class WiredInternalVariables
{
    private const double Hundredths = 100.0;
    private const string IdPrefix = "@internal.";

    private static WiredInternalVariable Furni(string name, Func<RoomItemStore.PlacedItem, IWiredRoom, int> read,
        Func<RoomItemStore.PlacedItem, int, IWiredRoom, Task<bool>>? write = null) =>
        new(Define(name, WiredVariableTargets.Furni, write is not null),
            (holder, _, room) => holder.Item is { } item ? read(item, room) : null,
            write is null ? null : (holder, value, room) => holder.Item is { } item ? write(item, value, room) : Task.FromResult(false));

    private static WiredInternalVariable User(string name, Func<RoomEntityStore.RoomEntity, IWiredRoom, int> read,
        Func<RoomEntityStore.RoomEntity, int, IWiredRoom, Task<bool>>? write = null) =>
        new(Define(name, WiredVariableTargets.User, write is not null),
            (holder, _, room) => holder.User is { } user ? read(user, room) : null,
            write is null ? null : (holder, value, room) => holder.User is { } user ? write(user, value, room) : Task.FromResult(false));

    private static WiredInternalVariable Global(string name, Func<IWiredRoom, int> read,
        Func<int, IWiredRoom, Task<bool>>? write = null) =>
        new(Define(name, WiredVariableTargets.Global, write is not null),
            (_, _, room) => read(room),
            write is null ? null : (_, value, room) => write(value, room));

    private static WiredInternalVariable Context(string name, Func<WiredRun, int?> read) =>
        new(Define(name, WiredVariableTargets.Context, writable: false),
            (_, run, _) => run is null ? null : read(run));

    /// <summary>The id of the internal variable of this name held by this target, whether this server keeps it or not.</summary>
    public static string IdOf(int target, string name) => IdPrefix + target + "." + name;

    private static WiredVariable Define(string name, int target, bool writable) => new(
        IdOf(target, name), WiredVariableKinds.Internal, name, WiredVariableAvailability.WhileRoomActive, target,
        AlwaysAvailable: true, CanCreateAndDelete: false, HasValue: true, CanWriteValue: writable,
        CanInterceptChanges: false, IsInvisible: false, CanReadCreationTime: false, CanReadLastUpdateTime: false);

    private static int Bool(bool value) => value ? 1 : 0;

    private static async Task<bool> MoveTo(RoomItemStore.PlacedItem item, int x, int y, int rotation, IWiredRoom room) =>
        await room.MoveItemAsync(item, x, y, rotation, WiredMoveOptions.Default);

    public static readonly IReadOnlyList<WiredInternalVariable> All =
    [
        // furni
        Furni("@id", (item, _) => item.Item.Id),
        Furni("@class_id", (item, _) => item.Item.Definition!.ClassId),
        Furni("@height", (item, _) => (int)Math.Round(item.Item.Definition!.StackHeight * Hundredths)),
        Furni("@state", (item, _) => int.TryParse(item.Data.StateText, out var state) ? state : 0,
            async (item, value, room) =>
            {
                await room.SetStateAsync(item, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return true;
            }),
        Furni("@position_x", (item, _) => item.X, (item, value, room) => MoveTo(item, value, item.Y, item.Rotation, room)),
        Furni("@position_y", (item, _) => item.Y, (item, value, room) => MoveTo(item, item.X, value, item.Rotation, room)),
        Furni("@rotation", (item, _) => item.Rotation, (item, value, room) => MoveTo(item, item.X, item.Y, value, room)),
        Furni("@altitude", (item, _) => (int)Math.Round(item.Z * Hundredths),
            (item, value, room) => room.SetAltitudeAsync(item, value / Hundredths)),
        Furni("@is_stackable", (item, _) => Bool(item.Item.Definition!.CanStackOn)),
        Furni("@can_stand_on", (item, _) => Bool(item.Item.Definition!.CanWalk)),
        Furni("@can_sit_on", (item, _) => Bool(item.Item.Definition!.CanSit)),
        Furni("@can_lay_on", (item, _) => Bool(item.Item.Definition!.CanLay)),
        Furni("@owner_id", (item, _) => item.Item.Owner?.AccountId ?? 0),

        // users
        User("@index", (user, _) => user.RoomIndex),
        User("@type", (user, _) => WiredUserKinds.Of(user)),
        User("@gender", (user, _) => user.Player.Gender == Gender.Female ? 1 : 0),
        User("@achievement_score", (user, _) => user.Player.AchievementScore),
        User("@is_hc", (user, _) => Bool(user.Player.ClubLevel != ClubLevel.None)),
        User("@has_rights", (user, _) => Bool(user.ControllerLevel >= RoomControllerLevels.Rights)),
        User("@is_owner", (user, room) => Bool(room.IsOwner(user))),
        User("@position_x", (user, _) => user.X, (user, value, room) => Warp(user, value, user.Y, room)),
        User("@position_y", (user, _) => user.Y, (user, value, room) => Warp(user, user.X, value, room)),
        User("@direction", (user, _) => user.BodyDirection, (user, value, room) =>
        {
            room.TurnUser(user, ((value % 8) + 8) % 8);
            return Task.FromResult(true);
        }),
        User("@altitude", (user, _) => (int)Math.Round(user.Z * Hundredths)),
        User("@effect_id", (user, _) => user.EffectId, (user, value, room) => room.SetEffectAsync(user, value)),
        User("@is_frozen", (user, _) => Bool(user.Frozen)),
        User("@is_muted", (user, room) => Bool(user.MutedUntil > room.Now().UtcDateTime)),
        User("@dance", (user, _) => user.DanceStyle, (user, value, room) => room.DanceAsync(user, value)),
        User("@sign", (user, _) => user.Sign ?? -1, (user, value, room) => room.ShowSignAsync(user, value)),
        User("@is_idle", (user, _) => Bool(user.Asleep)),
        User("@user_id", (user, _) => user.Player.AccountId),

        // the room
        Global("@furni_count", room => room.Items.Items.Count()),
        Global("@user_count", room => room.Entities.Count),
        Global("@wired_timer", room => room.TimerPulses),
        Global("@team_red_score", room => room.Games.Score(RoomGames.Red), (value, room) => SetScore(room, RoomGames.Red, value)),
        Global("@team_green_score", room => room.Games.Score(RoomGames.Green), (value, room) => SetScore(room, RoomGames.Green, value)),
        Global("@team_blue_score", room => room.Games.Score(RoomGames.Blue), (value, room) => SetScore(room, RoomGames.Blue, value)),
        Global("@team_yellow_score", room => room.Games.Score(RoomGames.Yellow), (value, room) => SetScore(room, RoomGames.Yellow, value)),
        Global("@team_red_size", room => room.Games.Members(RoomGames.Red).Count()),
        Global("@team_green_size", room => room.Games.Members(RoomGames.Green).Count()),
        Global("@team_blue_size", room => room.Games.Members(RoomGames.Blue).Count()),
        Global("@team_yellow_size", room => room.Games.Members(RoomGames.Yellow).Count()),
        Global("@room_id", room => room.RoomId),

        // a stack's run
        Context("@selector_furni_count", run => run.SelectedFurni.Count),
        Context("@selector_user_count", run => run.SelectedUsers.Count),
        Context("@signal_furni_count", run => run.Event.SignalFurni.Count),
        Context("@antenna_id", run => run.Event.Type == WiredEventType.SignalReceived ? run.Event.Item?.Item.Id : null),
        Context("@chat_type", run => run.Event.ChatType),
        Context("@chat_style", run => run.Event.ChatStyle),
        Context("@event.transaction_failed.reason", run => run.Event.FailureReason),
    ];

    private static Task<bool> Warp(RoomEntityStore.RoomEntity user, int x, int y, IWiredRoom room)
    {
        room.WarpUser(user, x, y);
        return Task.FromResult(true);
    }

    private static async Task<bool> SetScore(IWiredRoom room, int team, int value)
    {
        await room.Games.AddScoreAsync(team, value - room.Games.Score(team));
        return true;
    }

    private static readonly Dictionary<string, WiredInternalVariable> ById = All.ToDictionary(v => v.Definition.Id);

    public static WiredInternalVariable? Find(string id) => ById.GetValueOrDefault(id);
}
