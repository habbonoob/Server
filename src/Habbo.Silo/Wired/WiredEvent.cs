using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired;

/// <summary>Something that happened in a room, offered to every trigger: the user and the item
/// involved, and what was said, when those apply.</summary>
public sealed record WiredEvent(
    WiredEventType Type,
    RoomEntityStore.RoomEntity? User = null,
    RoomItemStore.PlacedItem? Item = null,
    string? Text = null)
{
    /// <summary>The action a user performed (a <see cref="WiredUserActions"/> code); its extra — which
    /// sign, which dance — is in <see cref="WiredEvent.Text"/>.</summary>
    public int? Action { get; init; }

    /// <summary>The team whose score changed, and its score before and after.</summary>
    public int? Team { get; init; }
    public int? OldScore { get; init; }
    public int? NewScore { get; init; }

    /// <summary>The user clicked, for a click on a user.</summary>
    public RoomEntityStore.RoomEntity? ClickedUser { get; init; }

    /// <summary>The user a bot reached.</summary>
    public RoomEntityStore.RoomEntity? ReachedUser { get; init; }

    /// <summary>The bot that reached somewhere.</summary>
    public RoomEntityStore.RoomEntity? Bot { get; init; }

    /// <summary>For a variable change: which variable, what happened, and its value before and after.</summary>
    public string? VariableId { get; init; }
    public Variables.WiredVariableChange? VariableChange { get; init; }
    public int? OldValue { get; init; }
    public int? NewValue { get; init; }

    /// <summary>For a variable change: where it came from (a <see cref="Variables.WiredVariableOrigins"/> bit).</summary>
    public int VariableOrigin { get; init; } = Variables.WiredVariableOrigins.ThisRoom;

    /// <summary>For something said: how (talked, whispered, shouted) and in which bubble.</summary>
    public int? ChatType { get; init; }
    public int? ChatStyle { get; init; }

    /// <summary>Why a transaction failed, as the client numbers the reasons.</summary>
    public int? FailureReason { get; init; }

    /// <summary>What a signal carried to the stacks that receive it.</summary>
    public IReadOnlyList<RoomItemStore.PlacedItem> SignalFurni { get; init; } = [];
    public IReadOnlyList<RoomEntityStore.RoomEntity> SignalUsers { get; init; } = [];

    /// <summary>For a signal: the execution that sent it, which the stacks receiving it carry on.</summary>
    public WiredExecution? Execution { get; init; }
}

public enum WiredEventType
{
    /// <summary>The room's half-second pulse, which drives every timer.</summary>
    Pulse,
    UserWalkedOn,
    UserWalkedOff,
    UserSaid,
    UserEntered,

    /// <summary>A user changed an item's state by using it.</summary>
    FurniStateChanged,

    /// <summary>A user waved, sat down, held up a sign, fell asleep, and so on.</summary>
    UserPerformedAction,

    /// <summary>A signal arrived on an antenna (the event's item).</summary>
    SignalReceived,

    /// <summary>Furni moved by wired ran into a user.</summary>
    Collision,

    GameStarted,
    GameEnded,
    ScoreChanged,

    /// <summary>A running clock (the event's item) counted a pulse.</summary>
    ClockTicked,

    UserLeft,

    /// <summary>A user clicked an item.</summary>
    FurniClicked,

    /// <summary>A user clicked another user (the event's clicked user).</summary>
    UserClicked,

    /// <summary>An item's state changed, whatever changed it.</summary>
    FurniStateSet,

    /// <summary>The room's 50 millisecond tick, for the short repeat trigger.</summary>
    ShortTick,

    /// <summary>A variable was given, changed or taken (from the event's user or item, or the room).</summary>
    VariableChanged,

    /// <summary>A user's wired transaction under a contract (the event's item) went through, or failed.</summary>
    TransactionCompleted,
    TransactionFailed,

    /// <summary>A bot ended a walk on the event's item.</summary>
    BotReachedFurni,

    /// <summary>A bot ended a walk next to the event's user.</summary>
    BotReachedUser,
}

/// <summary>One run of a stack: the event that started it, the room to act through, what the
/// stack's selectors chose, and what its effects moved. Conditions and effects find their targets
/// here, from the sources their dialogs name.</summary>
public sealed class WiredRun(IWiredRoom room, WiredEvent trigger)
{
    public IWiredRoom Room { get; } = room;
    public WiredEvent Event { get; } = trigger;

    /// <summary>The user the event was about.</summary>
    public RoomEntityStore.RoomEntity? User => Event.User;

    /// <summary>The item the event was about.</summary>
    public RoomItemStore.PlacedItem? Item => Event.Item;

    /// <summary>The furni the stack's selectors chose; empty when none chose any.</summary>
    public List<RoomItemStore.PlacedItem> SelectedFurni { get; set; } = [];

    /// <summary>The users the stack's selectors chose; empty when none chose any.</summary>
    public List<RoomEntityStore.RoomEntity> SelectedUsers { get; set; } = [];

    /// <summary>The stacks whose selectors this run is already choosing with, by tile, so remote
    /// selectors that reach each other stop.</summary>
    public HashSet<(int X, int Y)> VisitedStacks { get; init; } = [];

    /// <summary>The stack being run, lowest box first.</summary>
    public IReadOnlyList<WiredBox> Stack { get; set; } = [];

    /// <summary>The execution the run is part of: its budgets, and its context values.</summary>
    public WiredExecution Execution { get; set; } = new();

    /// <summary>The run's context variables and their values, shared by every stack of its execution.</summary>
    public Dictionary<string, Variables.WiredVariableValue> ContextValues => Execution.ContextValues;

    /// <summary>The moves each movement effect of the stack planned, by box, once planned.</summary>
    public Dictionary<int, IReadOnlyList<WiredMove>> PlannedMoves { get; } = [];

    /// <summary>The furni the stack's effects have moved so far.</summary>
    public List<RoomItemStore.PlacedItem> MovedFurni { get; } = [];

    /// <summary>The furni a box's furni slot works on, by the source its dialog chose. Furni no longer placed in the
    /// room (picked up while a delayed effect waited, say) are passed over, as users who left are.</summary>
    public IEnumerable<RoomItemStore.PlacedItem> Furni(WiredBox box, int slot = 0) => (box.FurniSource(slot) switch
    {
        WiredSources.TriggerFurni => Item is { } item ? [item] : [],
        WiredSources.PickedFurni2 => box.Settings.StuffIds2.Select(Room.FindItem).OfType<RoomItemStore.PlacedItem>(),
        WiredSources.SnapshotFurni => box.Settings.Snapshots.Select(s => Room.FindItem(s.ItemId)).OfType<RoomItemStore.PlacedItem>(),
        WiredSources.SelectedFurni => SelectedFurni,
        WiredSources.SignalFurni => Event.SignalFurni,
        WiredSources.AllFurni => Room.Items.Items.ToList(),
        WiredSources.MovedFurni => MovedFurni.ToList(),
        _ => Room.Picked(box),
    }).Where(item => Room.FindItem(item.Item.Id) == item);

    /// <summary>The users a box's user slot works on, by the source its dialog chose. Users who
    /// have left are passed over.</summary>
    public IEnumerable<RoomEntityStore.RoomEntity> Users(WiredBox box, int slot = 0) =>
        (box.UserSource(slot) switch
        {
            WiredSources.SelectedUsers => SelectedUsers,
            WiredSources.SignalUsers => Event.SignalUsers,
            WiredSources.ClickedUser => Event.ClickedUser is { } clicked ? [clicked] : [],
            WiredSources.ReachedUser => Event.ReachedUser is { } reached ? [reached] : [],
            WiredSources.BotByName => Room.Entities.Bots.Where(bot => WiredBots.Named(box, bot)).ToList(),
            WiredSources.UserByName => Room.Entities.Entities
                .Where(user => string.Equals(user.Player.Username, box.Settings.StringParam, StringComparison.OrdinalIgnoreCase)).ToList(),
            WiredSources.AllUsers => Room.Entities.Entities.ToList(),
            _ => User is { } user ? [user] : [],
        }).Where(Room.IsPresent);

    /// <summary>How the stack's add-ons say its effects move furni. Whether users block is the
    /// effect's to say first; the add-ons may only let them through.</summary>
    public WiredMoveOptions MoveOptions(bool avatarsBlock = true)
    {
        var options = WiredMoveOptions.Default with { AvatarsBlock = avatarsBlock };
        foreach (var box in Stack)
        {
            options = box.Type switch
            {
                Addons.AnimationTimeAddon => options with { AnimationMilliseconds = Addons.AnimationTimeAddon.Milliseconds(box) },
                Addons.NoMoveAnimationAddon => options with { AnimationMilliseconds = 0 },
                Addons.MovePhysicsAddon => Addons.MovePhysicsAddon.Apply(box, options),
                Addons.CarryUsersAddon => options with { CarryMode = box.IntParam(0) },
                Addons.MovementCurveAddon => options with { CurveStrength = Addons.MovementCurveAddon.CurveStrength(box, this) },
                _ => options,
            };
        }

        // no animation wins over a set time, wherever the two stand in the stack
        return Stack.Any(box => box.Type is Addons.NoMoveAnimationAddon) ? options with { AnimationMilliseconds = 0 } : options;
    }

    /// <summary>A wired text with the stack's placeholders, $(name), filled in.</summary>
    public string Fill(string text)
    {
        foreach (var box in Stack.Where(box => box.Type is Addons.WiredPlaceholderAddon))
        {
            var name = Addons.WiredPlaceholderAddon.Name(box);
            if (name.Length > 0 && text.Contains("$(" + name + ")", StringComparison.OrdinalIgnoreCase))
                text = text.Replace("$(" + name + ")", Clip(((Addons.WiredPlaceholderAddon)box.Type).Value(box, this), MaxPlaceholderLength),
                    StringComparison.OrdinalIgnoreCase);
            // a text made of the same placeholder over and over, standing for every furni in the room, grew to megabytes a run
            if (text.Length > MaxFilledLength)
                return text[..MaxFilledLength];
        }

        return text;
    }

    /// <summary>The longest a placeholder's value, and a filled-in text, may get: more than a chat bubble shows.</summary>
    private const int MaxPlaceholderLength = 256;
    private const int MaxFilledLength = 1024;

    private static string Clip(string text, int length) => text.Length > length ? text[..length] : text;

    /// <summary>Moves a furni for one of the stack's effects, and remembers it moved.</summary>
    public async Task<bool> MoveAsync(RoomItemStore.PlacedItem item, int x, int y, int rotation, bool avatarsBlock = true)
    {
        var options = MoveOptions(avatarsBlock);
        if (Stack.FirstOrDefault(box => box.Type is Addons.ProjectileAddon) is { } projectile && Addons.ProjectileAddon.Flies(projectile, this, item))
            (rotation, options) = Addons.ProjectileAddon.Shape(projectile, this, item, x, y, rotation, options);

        var moved = await Room.MoveItemAsync(item, x, y, rotation, options);
        if (moved && !MovedFurni.Contains(item))
            MovedFurni.Add(item);
        return moved;
    }
}
