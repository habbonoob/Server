using System.Globalization;
using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Infrastructure;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Room.Chat;
using Habbo.Protocol.Messages.Outgoing.Room.Engine;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;
using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Abstractions.Wired;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired.Addons;
using Habbo.Silo.Wired.Triggers;
using Habbo.Silo.Wired.Variables;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Silo.Wired;

/// <summary>Runs one room's wired. Boxes standing on the same tile are a stack: when one of a
/// stack's triggers matches what happened, its selectors choose the furni and users to work on,
/// every condition must pass, and then its effects run — now, or after their delay — each on the
/// targets its sources name. Add-ons in the stack change which effects run.
///
/// The grain feeds in what happens in the room (walks, chat, entries, uses, and its half-second
/// pulse) and supplies how to broadcast and move furniture, since both belong to it.</summary>
public sealed partial class RoomWired(
    Guid ownerId,
    RoomItemStore items,
    RoomEntityStore entities,
    RoomGames games,
    WiredBoxRegistry registry,
    IFurniRoom furniture,
    IDbContextFactory<AppDbContext> dbFactory,
    Action<IComposer> broadcast,
    Action<Guid, IComposer> sendTo,
    WiredRoomActions actions,
    ILogger logger) : IWiredRoom
{
    /// <summary>The stack runs a pulse counts as full load, for the monitor.</summary>
    private const int MonitorRunsPerPulse = 100;

    /// <summary>Stack runs one pulse may start, all executions together: a safety valve well above what one execution
    /// may (<see cref="WiredExecution"/>: a ring of 24 stacks makes 2400 visits), so only many at once reach it.</summary>
    private const int MaxRunsPerPulse = 5000;

    /// <summary>Furni saves, moves and kept variable writes one pulse may make, all executions together: a safety
    /// valve over the executions' own budgets, so a room full of them cannot keep the database busy.</summary>
    private const int MaxWritesPerPulse = 2000;

    /// <summary>The longest an effect may wait, in pulses: a bound, so the queue cannot fill with
    /// runs no one will see. The dialog does not say one.</summary>
    /// <summary>The longest delay an effect may have: the client offers 0 to 20 half-seconds.</summary>
    private const int MaxDelayInPulses = 20;

    /// <summary>The most variable definitions a room may keep permanently (Habbo refused the 23rd).</summary>
    private const int MaxPermanentVariables = 22;

    /// <summary>What a box that needs a permission says to one without it.</summary>
    private const string StaffOnly = "wiredfurni.error.staff_only";

    /// <summary>The most delayed effects that may wait at once; more are dropped, and counted.</summary>
    private const int MaxDelayedEffects = 1000;

    /// <summary>How much a pulse's runs count toward the monitor's average load.</summary>
    private const double LoadSmoothing = 0.1;

    /// <summary>The two looks a box flashes between when it runs, as Habbo shows wired working.</summary>
    private const string IdleState = "0";
    private const string ActiveState = "1";

    private readonly Dictionary<int, WiredBox> _boxes = [];
    private readonly List<(int Due, WiredBox Effect, WiredRun Run)> _delayed = [];
    private readonly List<(int Due, Func<Task> Action)> _scheduled = [];
    private Dictionary<(int X, int Y), List<WiredBox>>? _stacks;
    private int _pulse;
    private int _timerStart;
    private int _runsThisPulse;
    /// <summary>Runs set off by what visitors do, counted apart: one pacing on a plate or clicking a furni over and over
    /// spends these, not the runs the room's timers and repeaters need.</summary>
    private int _visitorRunsThisPulse;
    private int _writesThisPulse;
    private bool _listensForUserClicks;
    private readonly Dictionary<WiredErrorKind, WiredErrorCount> _errors = [];
    private double _load;
    private int _peakRuns;
    private bool _heavy;
    private WiredEvent? _firing;
    /// <summary>The event that started what is running now; a signal sent from it counts where it does.</summary>
    private WiredEvent? _root;
    /// <summary>The execution running now, whose budgets moves and variable operations spend.</summary>
    private WiredExecution? _execution;

    public RoomItemStore Items => items;
    public RoomEntityStore Entities => entities;
    public RoomGames Games => games;

    public IWiredChests Chests => actions.Chests;
    public Rewards.IWiredRewards Rewards => actions.Rewards;
    public int TimerPulses => _pulse - _timerStart;
    public Random Random => Random.Shared;

    /// <summary>The clock the time and date conditions read.</summary>
    public Func<DateTimeOffset> Clock { get; set; } = () => DateTimeOffset.UtcNow;

    public DateTimeOffset Now() => Clock();

    /// <summary>The room's wired time zone, as its settings (or the hotel's) give it.</summary>
    public string Timezone { get; set; } = string.Empty;

    public Task LogAsync(byte level, byte source, string message) => actions.Log(level, source, message);

    // ---------- the monitor ----------

    /// <summary>The monitor's usage: the most stack runs a recent pulse took, out of the most a
    /// pulse may; heavy while the average is over half that.</summary>
    public (double Cost, double Cap, bool Heavy) Usage => (_peakRuns, MonitorRunsPerPulse, _heavy);

    public IReadOnlyCollection<WiredErrorCount> Errors => _errors.Values;

    /// <summary>Every kind of error with how often it happened here (none for most).</summary>
    public IEnumerable<(WiredErrorKind Kind, int Count, DateTimeOffset? Last)> ErrorCounts =>
        WiredErrors.All.Select(kind => _errors.TryGetValue(kind, out var c) ? (kind, c.Count, (DateTimeOffset?)c.Last) : (kind, 0, null));

    public void ClearErrors() => _errors.Clear();

    private void Record(WiredErrorKind kind)
    {
        if (!_errors.TryGetValue(kind, out var count))
            _errors[kind] = count = new WiredErrorCount(kind);
        count.Count++;
        count.Last = Clock();
    }

    // ---------- the boxes in the room ----------

    /// <summary>Picks up every wired box among the room's items, with the settings saved for it.</summary>
    public async Task LoadAsync(AppDbContext db, CancellationToken ct = default)
    {
        var boxes = items.Items.Where(item => TypeOf(item) is not null).ToList();
        var ids = boxes.Select(item => item.Item.Id).ToList();
        var saved = await db.FurniWiredBoxes.AsNoTracking()
            .Include(w => w.Snapshots)
            .Where(w => ids.Contains(w.ItemId))
            .ToDictionaryAsync(w => w.ItemId, ct);

        foreach (var item in boxes)
            Add(item, saved.GetValueOrDefault(item.Item.Id));
    }

    /// <summary>An item was put down: if it is a wired box, it joins the room with the settings it
    /// kept from before it was picked up.</summary>
    public async Task PlacedAsync(RoomItemStore.PlacedItem item)
    {
        _stacks = null;

        await using var db = await dbFactory.CreateDbContextAsync();
        await LoadItemVariablesAsync(db, item);
        if (TypeOf(item) is null)
            return;

        var saved = await db.FurniWiredBoxes.AsNoTracking()
            .Include(w => w.Snapshots)
            .FirstOrDefaultAsync(w => w.ItemId == item.Item.Id);
        Add(item, saved);
        if (_boxes.GetValueOrDefault(item.Item.Id) is { Type: Addons.VariableFx.VariableFxAddon } display)
            DisplayPlaced(display);
        if (_boxes.GetValueOrDefault(item.Item.Id) is { Type: ReferenceVariableBox })
            await ReferencePlacedAsync();
    }

    /// <summary>An item moved: stacks may have changed.</summary>
    public void Moved() => _stacks = null;

    /// <summary>An item left the room. Its delayed effects are dropped when they come due; what it
    /// held is kept only where kept; and a variable box takes its variable with it.</summary>
    public async Task RemovedAsync(int itemId)
    {
        var removed = _boxes.GetValueOrDefault(itemId);
        if (removed?.Type is Addons.VariableFx.VariableFxAddon)
            DisplayRemoved(removed);
        _boxes.Remove(itemId);
        _stacks = null;
        TellClickListening();
        ForgetItem(itemId);
        if (removed?.Type is WiredVariableBox)
            await VariableRemovedAsync(removed.Id.ToString(CultureInfo.InvariantCulture));
    }

    public bool IsBox(int itemId) => _boxes.ContainsKey(itemId);

    private WiredBoxType? TypeOf(RoomItemStore.PlacedItem item) =>
        WiredKinds.FromInteraction(item.Item.Definition!.InteractionName) is not null
            ? registry.For(item.Item.Definition.ClassName)
            : null;

    /// <summary>Takes the item in as a box, if it is one the server implements, with its settings
    /// (a new box's when none were saved).</summary>
    public void Add(RoomItemStore.PlacedItem item, FurniWired? saved)
    {
        if (TypeOf(item) is not { } type)
            return;

        _boxes[item.Item.Id] = new WiredBox(item, type, saved ?? new FurniWired { ItemId = item.Item.Id });
        _stacks = null;
        TellClickListening();
    }

    /// <summary>Whether a box listens for clicks on users: the client then leaves turning and the
    /// avatar menu to the server.</summary>
    public bool ListensForUserClicks => _boxes.Values.Any(box => box.Type is ClickUserTrigger);

    /// <summary>What the client needs to know about the room's wired. No box awards achievements, so the list of
    /// achievements the room's wired may award is empty.</summary>
    public IComposer Environment() => new WiredEnvironmentMessageComposer(ListensForUserClicks, EnabledAchievements: []);

    private void TellClickListening()
    {
        if (ListensForUserClicks == _listensForUserClicks)
            return;

        _listensForUserClicks = !_listensForUserClicks;
        broadcast(Environment());
    }

    /// <summary>Whether a box repeats faster than the pulse, so the room must tick it.</summary>
    public bool HasShortTicks => _boxes.Values.Any(box => box.Type is RepeatShortTrigger);

    /// <summary>The room's stacks by tile, each lowest box first.</summary>
    private Dictionary<(int X, int Y), List<WiredBox>> Stacks => _stacks ??= _boxes.Values
        .Where(box => box.Item.Item.Definition!.Type == ItemType.Floor)
        .GroupBy(box => (box.Item.X, box.Item.Y))
        .ToDictionary(stack => stack.Key, stack => stack.OrderBy(box => box.Item.Z).ThenBy(box => box.Id).ToList());

    // ---------- dialogs ----------

    /// <summary>The message that opens the box's dialog, or null for anything that is not a box.</summary>
    public IComposer? DialogFor(int itemId) =>
        _boxes.GetValueOrDefault(itemId) is { } box ? WiredDialogs.For(box, items, DialogContext(box)) : null;

    /// <summary>Checks and stores a box's settings; the answer is the message for the dialog —
    /// saved, or why not. Null when the message was not about a box of that family at all.
    /// <paramref name="holds"/> tells whether the one saving holds a permission.</summary>
    public async Task<IComposer?> SaveAsync(WiredSaveData save, Func<string, bool> holds)
    {
        if (_boxes.GetValueOrDefault(save.ItemId) is not { } box || box.Type.Kind != save.Kind)
            return null;

        var type = box.Type;
        if (type.Permission is { } permission && !holds(permission))
            return WiredDialogs.Refusal(WiredValidation.Of(StaffOnly));
        if (save.IntParams.Count > WiredBoxType.MaxIntParams)
            return null;

        if (save.StringParam.Length > FurniWired.StringParamMaxLength)
        {
            return WiredDialogs.Refusal(new WiredValidation("wiredfurni.error.too_much_text",
                new Dictionary<string, string> { ["limit"] = FurniWired.StringParamMaxLength.ToString(CultureInfo.InvariantCulture) }));
        }

        // picks: each once, still in the room, and no more than a box may hold
        var picks = type.TakesPicks ? save.StuffIds.Distinct().Where(id => Pickable(type, id)).ToList() : [];
        var picks2 = type.TakesPicks ? save.StuffIds2.Distinct().Where(id => Pickable(type, id)).ToList() : [];
        // the dialog stops picking at the limit it was given, so more is not a dialog talking
        if (picks.Count > WiredBoxType.MaxPickedFurni || picks2.Count > WiredBoxType.MaxPickedFurni)
            return null;

        if (type.PickRule is { } rule && picks.Any(id => !rule.Accepts(items.Find(id)!)))
            return WiredDialogs.Refusal(WiredValidation.Of(rule.ErrorKey));

        var settings = new FurniWired
        {
            ItemId = box.Id,
            IntParams = save.IntParams,
            StringParam = save.StringParam,
            StuffIds = picks,
            StuffIds2 = picks2,
            FurniSources = save.FurniSources,
            UserSources = save.UserSources,
            VariableIds = save.VariableIds,
            DelayInPulses = save.DelayInPulses,
            Quantifier = save.Quantifier,
            IsFilter = save.IsFilter,
            IsInvert = save.IsInvert,
        };

        if ((Settle(type, settings) ?? type.ValidateInStack(settings, box, StackAt(box.Item.X, box.Item.Y) ?? [])
                ?? await type.ValidateAsync(settings, this)) is { } refusal)
            return WiredDialogs.Refusal(refusal);

        if (type.TakesSnapshot)
            settings.Snapshots = picks.Select(id => items.Find(id)!).Select(item => Snapshot(box.Id, item)).ToList();

        if (type is WiredVariableBox variableBox)
        {
            // a name no other variable in the room has, whatever it belongs to (Habbo refused a context variable
            // named as a furni one); and a variable whose values change how they last loses the values it had
            var others = _boxes.Values.Where(other => other != box && other.Type is WiredVariableBox).ToList();
            if (others.Any(other => string.Equals(other.Settings.StringParam, settings.StringParam, StringComparison.OrdinalIgnoreCase)))
                return WiredDialogs.Refusal(WiredValidation.Of("wiredfurni.error.variables.name_uniq"));

            var before = variableBox.Define(box);
            var after = variableBox.Define(new WiredBox(box.Item, type, settings));

            // Habbo refused the 23rd permanent definition in a room
            if (WiredVariableAvailability.IsPermanent(after.Availability)
                && others.Count(other => WiredVariableAvailability.IsPermanent(((WiredVariableBox)other.Type).Define(other).Availability))
                >= MaxPermanentVariables)
                return WiredDialogs.Refusal(WiredValidation.Of("wiredfurni.error.variables.permanent_limit"));
            if (before.Availability != after.Availability || before.HasValue != after.HasValue)
                await ClearVariableAsync(before.Id);
        }

        await StoreAsync(settings);
        box.Settings = settings;
        if (type is Addons.VariableFx.VariableFxAddon)
            DisplaySaved(box);
        if (type is ReferenceVariableBox)
            await ReferencePlacedAsync();
        box.ResetRunState();
        return new WiredSaveSuccessComposer();
    }

    /// <summary>What a save makes of a box's settings without asking the room: a source per slot the box offers, an
    /// effect's delay within what the dialog offers, and then the box's own checks (null when they pass). The room
    /// import sets copied boxes up through it too.</summary>
    public static WiredValidation? Settle(WiredBoxType type, FurniWired settings)
    {
        settings.FurniSources = Sources(settings.FurniSources, type.FurniSlots);
        settings.UserSources = Sources(settings.UserSources, type.UserSlots);
        settings.DelayInPulses = type.Kind == WiredKind.Effect ? Math.Clamp(settings.DelayInPulses, 0, MaxDelayInPulses) : 0;
        return type.Validate(settings);
    }

    /// <summary>One source per slot, each one the slot offers; anything else is the slot's default.</summary>
    private static List<int> Sources(IReadOnlyList<int> chosen, IReadOnlyList<WiredSlot> slots) =>
        slots.Select((slot, index) => index < chosen.Count && slot.Sources.Contains(chosen[index]) ? chosen[index] : slot.Default)
            .ToList();

    private static FurniWiredSnapshot Snapshot(int wiredItemId, RoomItemStore.PlacedItem item) => new()
    {
        WiredItemId = wiredItemId,
        ItemId = item.Item.Id,
        State = item.Data.StateText,
        X = item.X,
        Y = item.Y,
        Z = item.Z,
        Rotation = item.Rotation,
    };

    private async Task StoreAsync(FurniWired settings)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        // the snapshot is replaced whole; its rows are keyed by item, so the old ones go first
        await db.FurniWiredSnapshots.Where(s => s.WiredItemId == settings.ItemId).ExecuteDeleteAsync();
        var row = await db.FurniWiredBoxes.FirstOrDefaultAsync(w => w.ItemId == settings.ItemId);
        if (row is null)
        {
            row = new FurniWired { ItemId = settings.ItemId };
            db.FurniWiredBoxes.Add(row);
        }

        row.IntParams = [.. settings.IntParams];
        row.StringParam = settings.StringParam;
        row.StuffIds = [.. settings.StuffIds];
        row.StuffIds2 = [.. settings.StuffIds2];
        row.FurniSources = [.. settings.FurniSources];
        row.UserSources = [.. settings.UserSources];
        row.VariableIds = [.. settings.VariableIds];
        row.DelayInPulses = settings.DelayInPulses;
        row.Quantifier = settings.Quantifier;
        row.IsFilter = settings.IsFilter;
        row.IsInvert = settings.IsInvert;
        foreach (var snapshot in settings.Snapshots)
        {
            db.FurniWiredSnapshots.Add(new FurniWiredSnapshot
            {
                WiredItemId = snapshot.WiredItemId, ItemId = snapshot.ItemId, State = snapshot.State,
                X = snapshot.X, Y = snapshot.Y, Z = snapshot.Z, Rotation = snapshot.Rotation,
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>The "apply" button of a match-to-snapshot effect's dialog.</summary>
    public Task ApplySnapshotAsync(int itemId) =>
        _boxes.GetValueOrDefault(itemId) is { Type: Effects.MatchSnapshotEffect } box
            ? Effects.MatchSnapshotEffect.Restore(box, this)
            : Task.CompletedTask;

    // ---------- what happens in the room ----------

    public async Task UserEnteredAsync(RoomEntityStore.RoomEntity user)
    {
        await LoadUserVariablesAsync(user);
        WelcomeToDisplays(user);
        await FireAsync(new WiredEvent(WiredEventType.UserEntered, user));
    }

    /// <summary>Whether a keyword trigger that hides what it hears matches this: the room then
    /// shows the message only to its speaker. Asked before the message goes out.</summary>
    public bool Hides(RoomEntityStore.RoomEntity user, string text)
    {
        var said = new WiredEvent(WiredEventType.UserSaid, user, Text: text);
        return _boxes.Values.Any(box =>
            box.Type is Triggers.SaysKeywordTrigger trigger && Triggers.SaysKeywordTrigger.Hides(box) && trigger.Matches(box, said, this));
    }

    /// <summary>How something was said (the @chat_type the stack's run is told).</summary>
    public const int Talked = 0;

    /// <summary>Offers what a user said to the keyword triggers, after the message went out.</summary>
    /// <summary>A user's transaction under a contract went through.</summary>
    public Task TransactionCompletedAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem contract) =>
        FireAsync(new WiredEvent(WiredEventType.TransactionCompleted, user, contract));

    /// <summary>A user's transaction under a contract failed, for the reason given.</summary>
    public Task TransactionFailedAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem contract, int reason) =>
        FireAsync(new WiredEvent(WiredEventType.TransactionFailed, user, contract) { FailureReason = reason });

    public Task UserSaidAsync(RoomEntityStore.RoomEntity user, string text, int chatType = Talked, int chatStyle = 0) =>
        FireAsync(new WiredEvent(WiredEventType.UserSaid, user, Text: text) { ChatType = chatType, ChatStyle = chatStyle });

    /// <summary>A user stepped from one tile to another: they walked off what was under them and
    /// onto what is under them now.</summary>
    public async Task UserSteppedAsync(RoomEntityStore.RoomEntity user, int fromX, int fromY)
    {
        if (_boxes.Count == 0)
            return;

        foreach (var item in items.FloorItemsOn(fromX, fromY).ToList())
            await FireAsync(new WiredEvent(WiredEventType.UserWalkedOff, user, item));
        foreach (var item in items.FloorItemsOn(user.X, user.Y).ToList())
            await FireAsync(new WiredEvent(WiredEventType.UserWalkedOn, user, item));
    }

    /// <summary>What happened in the room's games.</summary>
    public Task GameEventAsync(RoomGames.GameEvent e) => e.Type switch
    {
        RoomGames.GameEventType.Started => FireAsync(new WiredEvent(WiredEventType.GameStarted)),
        RoomGames.GameEventType.Ended => FireAsync(new WiredEvent(WiredEventType.GameEnded)),
        RoomGames.GameEventType.ScoreChanged => FireAsync(new WiredEvent(WiredEventType.ScoreChanged)
            { Team = e.Team, OldScore = e.OldScore, NewScore = e.NewScore }),
        _ when e.Clock is { } clock => FireAsync(new WiredEvent(WiredEventType.ClockTicked, Item: clock.Item)),
        _ => Task.CompletedTask,
    };

    /// <summary>A user performed an action (a <see cref="WiredUserActions"/> code, with its extra).</summary>
    public Task UserActedAsync(RoomEntityStore.RoomEntity user, int action, string extra) =>
        FireAsync(new WiredEvent(WiredEventType.UserPerformedAction, user, Text: extra) { Action = action });

    public Task FurniStateChangedAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item) =>
        FireAsync(new WiredEvent(WiredEventType.FurniStateChanged, user, item));

    /// <summary>An item's state changed, by whatever means. A box's own flashing is not a change.</summary>
    public Task FurniStateSetAsync(RoomItemStore.PlacedItem item) =>
        _boxes.Count == 0 || IsBox(item.Item.Id)
            ? Task.CompletedTask
            : FireAsync(new WiredEvent(WiredEventType.FurniStateSet, Item: item));

    public async Task UserLeftAsync(RoomEntityStore.RoomEntity user)
    {
        if (_boxes.Count > 0)
            await FireAsync(new WiredEvent(WiredEventType.UserLeft, user));
        ForgetUser(user);
    }

    public Task FurniClickedAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item) =>
        _boxes.Count == 0 ? Task.CompletedTask : FireAsync(new WiredEvent(WiredEventType.FurniClicked, user, item));

    /// <summary>A user clicked another. Answers whether the avatar menu may open, and whether the
    /// clicker turns to face who they clicked: yes to both, unless a click box says otherwise.</summary>
    public async Task<(bool OpenMenu, bool Turn)> UserClickedAsync(RoomEntityStore.RoomEntity clicker, RoomEntityStore.RoomEntity clicked)
    {
        var listening = _boxes.Values.Where(box => box.Type is ClickUserTrigger).ToList();
        var answer = (!listening.Any(ClickUserTrigger.BlocksMenu), !listening.Any(ClickUserTrigger.KeepsDirection));
        if (listening.Count > 0)
            await FireAsync(new WiredEvent(WiredEventType.UserClicked, clicker) { ClickedUser = clicked });
        return answer;
    }

    /// <summary>The room's 50 millisecond tick, for the short repeat triggers.</summary>
    public Task ShortTickAsync() =>
        HasShortTicks ? FireAsync(new WiredEvent(WiredEventType.ShortTick)) : Task.CompletedTask;

    /// <summary>The room's half-second pulse: delayed effects that are due run, then the timers fire.</summary>
    public Task PulseAsync() => furniture.BatchedAsync(PulseStacksAsync);

    private async Task PulseStacksAsync()
    {
        _pulse++;
        _load = _load * (1 - LoadSmoothing) + (_runsThisPulse + _visitorRunsThisPulse) * LoadSmoothing;
        _peakRuns = Math.Max((int)Math.Round(_peakRuns * (1 - LoadSmoothing)), _runsThisPulse + _visitorRunsThisPulse);
        var heavy = _load > MonitorRunsPerPulse / 2.0;
        if (heavy && !_heavy)
            Record(WiredErrors.Heavy);
        _heavy = heavy;
        _runsThisPulse = 0;
        _visitorRunsThisPulse = 0;
        _writesThisPulse = 0;

        // what was scheduled (a teleport's end, say) still happens when the last box is gone meanwhile
        var dueActions = _scheduled.Where(entry => entry.Due <= _pulse).ToList();
        _scheduled.RemoveAll(entry => entry.Due <= _pulse);
        foreach (var (_, action) in dueActions)
            await action();

        if (_boxes.Count == 0)
            return;

        var due = _delayed.Where(entry => entry.Due <= _pulse).ToList();
        _delayed.RemoveAll(entry => entry.Due <= _pulse);
        foreach (var (_, effect, run) in due)
        {
            // an effect that waited starts a new execution, with fresh budgets and the same context values
            run.Execution = run.Execution.Continued();
            await InExecutionAsync(run.Execution, () => RunEffectAsync(effect, run));
        }

        await FireAsync(new WiredEvent(WiredEventType.Pulse));

        if (_pulse % ReferenceRefreshPulses == 0)
            await RefreshReferencesAsync();
    }

    // ---------- running stacks ----------

    private Task FireAsync(WiredEvent e) => furniture.BatchedAsync(() => FireEventAsync(e));

    private async Task FireEventAsync(WiredEvent e)
    {
        var outer = _firing;
        var outerRoot = _root;
        _firing = e;
        _root ??= e;
        try
        {
            await FireStacksAsync(e);
        }
        finally
        {
            _firing = outer;
            _root = outerRoot;
        }
    }

    private async Task FireStacksAsync(WiredEvent e)
    {
        // what an execution sets off (a signal, a variable it changed, a state it set) is part of it: the stacks
        // that hear it decide now, on the room as it is at this moment (Habbo: a signal's receivers see the state
        // as it was when it was sent, p35_flat), and act when the execution gets to them. Acting in turn rather
        // than inside the sender keeps a ring of 24 stacks, 2400 visits, from nesting 2400 deep.
        var setOff = _execution is not null;

        foreach (var (tile, stack) in Stacks.ToList())
        {
            // one stack that fails (a box set to something its code cannot handle) does not keep the
            // others from hearing the event, nor fail whatever set it off
            try
            {
                // every trigger hears the event, so each periodic one counts its own pulses; the stack
                // runs once however many of them matched
                var matched = stack.Where(box => box.Type is WiredTrigger trigger && trigger.Matches(box, e, this)).ToList();
                if (matched.Count == 0)
                    continue;

                // what happens on its own starts an execution per stack
                var run = new WiredRun(this, e) { Execution = e.Execution ?? _execution ?? new WiredExecution() };
                if (await InExecutionAsync(run.Execution, () => DecideStackAsync(tile, stack, matched, run)) is not { } passed)
                    continue;

                if (setOff)
                    run.Execution.Pending.Enqueue(() => ActAsync(tile, stack, run, passed, e.Type));
                else
                    await InExecutionAsync(run.Execution, () => ActAsync(tile, stack, run, passed, e.Type));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Wired stack at {X},{Y} failed on {Event}", tile.X, tile.Y, e.Type);
            }
        }
    }

    /// <summary>A decided stack's effects when its conditions held; its negative effects when they did not.</summary>
    private async Task ActAsync((int X, int Y) tile, List<WiredBox> stack, WiredRun run, bool passed, WiredEventType type)
    {
        try
        {
            await RunEffectsAsync(stack, run, negative: !passed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Wired stack at {X},{Y} failed on {Event}", tile.X, tile.Y, type);
        }
    }

    /// <summary>Runs something as part of an execution, so what it moves and the variables it changes spend that
    /// execution's budgets. The outermost one then works through what the execution set off, in the order it was
    /// set off, until nothing is left.</summary>
    private async Task<T> InExecutionAsync<T>(WiredExecution execution, Func<Task<T>> action)
    {
        var outer = _execution;
        _execution = execution;
        try
        {
            var result = await action();
            if (outer is null)
            {
                while (!execution.Stopped && execution.Pending.TryDequeue(out var next))
                    await next();
                execution.Pending.Clear();
            }

            return result;
        }
        finally
        {
            _execution = outer;
        }
    }

    private Task InExecutionAsync(WiredExecution execution, Func<Task> action) =>
        InExecutionAsync(execution, async () =>
        {
            await action();
            return true;
        });

    /// <summary>Gets a set-off stack ready to act: its captures, its selectors' choice, and whether its conditions
    /// hold. Null when it does not run at all (a budget spent, or its execution limit reached).</summary>
    private async Task<bool?> DecideStackAsync((int X, int Y) tile, List<WiredBox> stack, List<WiredBox> triggers, WiredRun run)
    {
        if (!TakeVisit(run.Execution, tile) || !TakeRun())
            return null;

        foreach (var trigger in triggers)
            Flash(trigger);

        if (stack.FirstOrDefault(box => box.Type is ExecutionLimitAddon) is { } limit && !ExecutionLimitAddon.TakeRun(limit, _pulse))
            return null;

        run.Stack = stack;
        foreach (var capturer in stack.Where(box => box.Type is VariableCapturerAddon))
            await VariableCapturerAddon.CaptureAsync(capturer, triggers, run);
        foreach (var scanner in stack.Where(box => box.Type is ChestItemTypeScannerAddon))
            await ChestItemTypeScannerAddon.ScanAsync(scanner, run);
        WiredSelection.Run(stack, run);
        if (stack.FirstOrDefault(box => box.Type is FurniVariableFilterAddon) is { } furniByVariable)
            run.SelectedFurni = VariableFilter.Filter(furniByVariable, run, run.SelectedFurni, WiredHolder.Of);
        if (stack.FirstOrDefault(box => box.Type is UserVariableFilterAddon) is { } usersByVariable)
            run.SelectedUsers = VariableFilter.Filter(usersByVariable, run, run.SelectedUsers, WiredHolder.Of);
        if (stack.FirstOrDefault(box => box.Type is FurniFilterAddon) is { } furniFilter)
            run.SelectedFurni = WiredFilter.Filter(furniFilter, run, run.SelectedFurni);
        if (stack.FirstOrDefault(box => box.Type is UserFilterAddon) is { } userFilter)
            run.SelectedUsers = WiredFilter.Filter(userFilter, run, run.SelectedUsers);

        // the stack's effects run when its conditions hold; its negative effects when they do not
        return ConditionsPass(stack, run);
    }

    /// <summary>Lets the selectors choose, lowest first: a selector adds what it chooses, or, as a
    /// filter, keeps only what it chooses of what the ones below chose; inverted, it chooses
    /// everything it would not.</summary>
    /// <summary>Whether the stack's conditions let it run: all of them, unless a condition
    /// evaluation add-on asks for another count of those it applies to.</summary>
    private static bool ConditionsPass(List<WiredBox> stack, WiredRun run)
    {
        var conditions = stack.Where(box => box.Type is WiredCondition).ToList();
        bool Passes(WiredBox box) => ((WiredCondition)box.Type).Passes(box, run);

        if (stack.FirstOrDefault(box => box.Type is ConditionEvaluationAddon) is not { } evaluation)
            return conditions.All(Passes);

        var picked = evaluation.Settings.StuffIds;
        var counted = picked.Count == 0 ? conditions : conditions.Where(box => picked.Contains(box.Id)).ToList();
        return conditions.Except(counted).All(Passes)
               && ConditionEvaluationAddon.Holds(evaluation, counted.Count(Passes), counted.Count);
    }

    private async Task RunEffectsAsync(List<WiredBox> stack, WiredRun run, bool negative = false)
    {
        var effects = stack.Where(box => box.Type is WiredEffect effect && effect.IsNegative == negative).ToList();
        if (effects.Count == 0)
            return;

        if (stack.FirstOrDefault(box => box.Type is RandomEffectAddon) is { } random)
            effects = RandomEffectAddon.Choose(random, effects, Random);
        else if (stack.FirstOrDefault(box => box.Type is UnseenEffectAddon) is { } unseen)
            effects = [UnseenEffectAddon.Next(unseen, effects)];

        // in the order they are stacked only when an add-on asks for it
        if (!stack.Any(box => box.Type is ExecuteInOrderAddon))
            effects = effects.OrderBy(_ => Random.Next()).ToList();

        foreach (var effect in effects)
        {
            // bounded here too: a row saved before the bound was kept may hold any delay
            var delay = Math.Clamp(effect.Settings.DelayInPulses, 0, MaxDelayInPulses);
            if (delay > 0)
            {
                if (_delayed.Count >= MaxDelayedEffects)
                {
                    Record(WiredErrors.DelayedEventLimit);
                    continue;
                }

                _delayed.Add((_pulse + delay, effect, run));
            }
            else
                await RunEffectAsync(effect, run);
        }
    }

    private async Task RunEffectAsync(WiredBox effect, WiredRun run)
    {
        // picked up, or picked up and put down again, while it waited; or its execution was stopped
        if (_boxes.GetValueOrDefault(effect.Id) != effect || run.Execution.Stopped)
            return;

        Flash(effect);
        try
        {
            await ((WiredEffect)effect.Type).RunAsync(effect, run);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Wired effect {ItemId} ({Type}) failed", effect.Id, effect.Type.GetType().Name);
        }
    }

    public Task RunStackEffectsAsync(RoomItemStore.PlacedItem item, WiredRun run)
    {
        if (Stacks.GetValueOrDefault((item.X, item.Y)) is not { } stack || !TakeVisit(run.Execution, (item.X, item.Y)) || !TakeRun())
            return Task.CompletedTask;
        if (stack.FirstOrDefault(box => box.Type is ExecutionLimitAddon) is { } limit && !ExecutionLimitAddon.TakeRun(limit, _pulse))
            return Task.CompletedTask;

        // the stack runs as itself: its own add-ons (movement, animation, placeholders) count, not the caller's; what
        // the caller chose and its context values come along
        var child = new WiredRun(this, run.Event)
        {
            Stack = stack,
            SelectedFurni = run.SelectedFurni.ToList(),
            SelectedUsers = run.SelectedUsers.ToList(),
            VisitedStacks = run.VisitedStacks,
            Execution = run.Execution,
        };
        // in its turn, like a signal's receivers, so stacks that call each other do not nest
        run.Execution.Pending.Enqueue(() => ActAsync((item.X, item.Y), stack, child, passed: true, run.Event.Type));
        return Task.CompletedTask;
    }

    /// <summary>Whether what runs now was set off by a visitor rather than by the room's own clock.</summary>
    private bool VisitorDriven => _root?.Type is WiredEventType.UserWalkedOn or WiredEventType.UserWalkedOff or WiredEventType.UserSaid
        or WiredEventType.UserEntered or WiredEventType.UserLeft or WiredEventType.UserPerformedAction
        or WiredEventType.FurniClicked or WiredEventType.UserClicked or WiredEventType.FurniStateChanged
        or WiredEventType.TransactionCompleted or WiredEventType.TransactionFailed;

    private bool RunsSpent => (VisitorDriven ? _visitorRunsThisPulse : _runsThisPulse) > MaxRunsPerPulse;

    private bool TakeRun()
    {
        ref var runs = ref VisitorDriven ? ref _visitorRunsThisPulse : ref _runsThisPulse;
        if (++runs <= MaxRunsPerPulse)
            return true;

        if (runs == MaxRunsPerPulse + 1)
        {
            logger.LogWarning("Wired stopped after {Runs} stack runs in one pulse; it is setting itself off", MaxRunsPerPulse);
            var signal = _firing?.Type == WiredEventType.SignalReceived;
            Record(signal ? WiredErrors.Recursion : WiredErrors.ExecutionLimit);
            _ = LogAsync(WiredLogLevels.Warn, WiredLogLevels.SystemSource,
                $"Wired stopped after {MaxRunsPerPulse} stack runs in one pulse" + (signal ? ": signals keep setting off stacks." : "."));
        }

        return false;
    }

    /// <summary>Whether the execution may run the stack on that tile once more; past its budget it is dropped,
    /// counted once.</summary>
    private bool TakeVisit(WiredExecution execution, (int X, int Y) tile)
    {
        if (execution.TakeVisit(tile, out var first))
            return true;
        if (first)
        {
            Record(WiredErrors.ExecutionLimit);
            _ = LogAsync(WiredLogLevels.Warn, WiredLogLevels.SystemSource, string.Create(CultureInfo.InvariantCulture,
                $"The stack at {tile.X},{tile.Y} ran {WiredExecution.MaxVisitsPerStack} times in one execution; it is setting itself off."));
        }

        return false;
    }

    /// <summary>Whether the execution running now may move one more furni; past its budget the rest are dropped.</summary>
    private bool TakeMove()
    {
        if (_execution is null || _execution.TakeMove(out var first))
            return true;
        if (first)
            Record(WiredErrors.ExecutionLimit);
        return false;
    }

    /// <summary>Whether the execution running now may make one more variable operation; the one past its budget stops
    /// it (Habbo raises EXECUTION_CAP).</summary>
    private bool TakeVariableOp()
    {
        if (_execution is null || _execution.TakeVariableOp(out var first))
            return true;
        if (first)
            Record(WiredErrors.ExecutionLimit);
        return false;
    }

    /// <summary>Whether the pulse may still make a write; past its budget the rest are dropped, once said.</summary>
    private bool TakeWrite()
    {
        if (++_writesThisPulse <= MaxWritesPerPulse)
            return true;

        if (_writesThisPulse == MaxWritesPerPulse + 1)
        {
            logger.LogWarning("Wired stopped after {Writes} furni and variable writes in one pulse", MaxWritesPerPulse);
            Record(WiredErrors.ExecutionLimit);
        }

        return false;
    }

    /// <summary>Shows a box working: it flips between its two looks each time it runs. Only the
    /// room sees this; the box's stored data does not change.</summary>
    private void Flash(WiredBox box) => FlashItem(box.Item);

    private void FlashItem(RoomItemStore.PlacedItem item)
    {
        item.Data = item.Data with { State = item.Data.State == ActiveState ? IdleState : ActiveState };
        furniture.ShowData(item);
    }

    // ---------- what boxes may do ----------

    public bool IsOwner(RoomEntityStore.RoomEntity user) => user.Player.Id == ownerId;

    public bool IsPresent(RoomEntityStore.RoomEntity user) => user switch
    {
        { Bot: { } bot } => entities.FindBot(bot.Data.Id) == user,
        { Pet: { } pet } => entities.FindPet(pet.Data.Id) == user,
        _ => entities.Find(user.Player.Id) == user,
    };

    public RoomItemStore.PlacedItem? FindItem(int itemId) => items.Find(itemId);

    /// <summary>Whether a box may hold this pick: still in the room, not a temporary copy (whose id is used again once it
    /// is gone), and on the wall only where its dialog allows it.</summary>
    private bool Pickable(WiredBoxType type, int itemId) =>
        items.Find(itemId) is { Temporary: false } item && (type.AllowsWallFurni || item.Item.Definition!.Type != ItemType.Wall);

    public IReadOnlyList<WiredBox>? StackAt(int x, int y) => Stacks.GetValueOrDefault((x, y));

    /// <summary>What a stack's selectors chose for the event last asked about, so the stack's triggers share one pass.</summary>
    private (WiredEvent Event, IReadOnlyList<WiredBox> Stack, IReadOnlyList<RoomItemStore.PlacedItem> Furni,
        IReadOnlyList<RoomEntityStore.RoomEntity> Users)? _selected;

    /// <summary>What the trigger's stack's selectors choose for the event: once per event and stack, counted as a stack
    /// run since it is one's work, and nothing once the pulse has run all it may.</summary>
    public (IReadOnlyList<RoomItemStore.PlacedItem> Furni, IReadOnlyList<RoomEntityStore.RoomEntity> Users) SelectedFor(WiredBox trigger, WiredEvent e)
    {
        if (StackAt(trigger.Item.X, trigger.Item.Y) is not { } stack)
            return ([], []);
        if (_selected is { } selected && ReferenceEquals(selected.Event, e) && selected.Stack == stack)
            return (selected.Furni, selected.Users);
        if (!TakeRun())
            return ([], []);

        var run = new WiredRun(this, e) { Stack = stack, Execution = e.Execution ?? _execution ?? new WiredExecution() };
        WiredSelection.Run(stack, run);
        _selected = (e, stack, run.SelectedFurni, run.SelectedUsers);
        return (run.SelectedFurni, run.SelectedUsers);
    }

    public IEnumerable<RoomItemStore.PlacedItem> Picked(WiredBox box) =>
        box.Settings.StuffIds.Select(items.Find).OfType<RoomItemStore.PlacedItem>();

    public Task SetStateAsync(RoomItemStore.PlacedItem item, string state) =>
        TakeWrite() ? furniture.UpdateAsync(item, item.Data with { State = state }) : Task.CompletedTask;

    public async Task<bool> MoveItemAsync(RoomItemStore.PlacedItem item, int x, int y, int rotation, WiredMoveOptions options)
    {
        if (!TakeMove() || !TakeWrite())
            return false;
        var moved = await actions.MoveItem(item, x, y, rotation, options);
        if (moved)
            _stacks = null;
        return moved;
    }

    public bool CanMoveItem(RoomItemStore.PlacedItem item, int x, int y, int rotation, WiredMoveOptions options) =>
        item.Item.Definition!.Type == ItemType.Floor
        && items.CanPlaceFloorItem(item.Item.Id, item.Item.Definition, x, y, rotation,
            options.AvatarsBlock ? entities.HasAvatarAt : (_, _) => false, options.ThroughFurni, options.BlockedByFurni);

    public async Task<bool> SetAltitudeAsync(RoomItemStore.PlacedItem item, double altitude)
    {
        if (!TakeMove() || !TakeWrite())
            return false;
        var moved = await actions.SetAltitude(item, altitude);
        if (moved)
            _stacks = null;
        return moved;
    }

    public void WarpUser(RoomEntityStore.RoomEntity user, int x, int y, (int X, int Y)? keepWalkingTo = null)
    {
        if (!IsPresent(user))
            return;

        if (user.Frozen && user.UnfreezeOnTeleport)
            Unfreeze(user);
        broadcast(UserUpdateMessageComposer.From([entities.Warp(user, x, y, keepWalkingTo)]));
    }

    public void SlideUser(RoomEntityStore.RoomEntity user, int x, int y, int animationMilliseconds, int? jumpPower = null)
    {
        if (!IsPresent(user) || user.Frozen)
            return;

        var (fromX, fromY, fromZ) = (user.X, user.Y, user.Z);
        if (entities.Slide(user, x, y) is null)
            return;

        broadcast(new WiredMovementsMessageComposer([
            new WiredUserMoveData(fromX, fromY, user.X, user.Y, RoomItemStore.FormatHeight(fromZ), RoomItemStore.FormatHeight(user.Z),
                user.RoomIndex, WiredUserMoveData.Slide, animationMilliseconds, user.BodyDirection, user.HeadDirection, jumpPower),
        ]));
    }

    public void TurnUser(RoomEntityStore.RoomEntity user, int direction)
    {
        if (!IsPresent(user))
            return;

        entities.Turn(user, direction);
        broadcast(new WiredMovementsMessageComposer([new WiredUserDirectionData(user.RoomIndex, user.BodyDirection, user.HeadDirection)]));
    }

    /// <summary>The game texts name each freeze option's effect: "${fx_218}".</summary>
    private const string FreezeEffectKey = "wiredfurni.params.freeze.effect.";

    public async Task FreezeAsync(RoomEntityStore.RoomEntity user, int effectOption, bool unfreezeOnTeleport)
    {
        if (!IsPresent(user))
            return;

        var text = await actions.Texts.GetAsync(FreezeEffectKey + effectOption.ToString(CultureInfo.InvariantCulture)) ?? string.Empty;
        var effect = int.TryParse(text.Replace("${fx_", string.Empty).TrimEnd('}'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : 0;
        user.Frozen = true;
        user.UnfreezeOnTeleport = unfreezeOnTeleport;
        user.Path.Clear();
        user.Goal = null;
        actions.ApplyEffect(user, effect);
    }

    public void Unfreeze(RoomEntityStore.RoomEntity user)
    {
        if (!user.Frozen)
            return;

        user.Frozen = false;
        user.UnfreezeOnTeleport = false;
        actions.ApplyEffect(user, 0);
    }

    public void Mute(RoomEntityStore.RoomEntity user, int minutes) =>
        user.MutedUntil = minutes <= 0 ? DateTime.MinValue : DateTime.UtcNow.AddMinutes(minutes);

    public Task KickAsync(RoomEntityStore.RoomEntity user) => IsPresent(user) ? actions.Kick(user) : Task.CompletedTask;

    public void AfterPulses(int pulses, Func<Task> action) => _scheduled.Add((_pulse + Math.Max(1, pulses), action));

    public Task<int?> FindEffectAsync(string library) => actions.Effects.FindAsync(library);

    public Task<bool> SetEffectAsync(RoomEntityStore.RoomEntity user, int effectId)
    {
        if (!IsPresent(user) || effectId < 0)
            return Task.FromResult(false);

        actions.ApplyEffect(user, effectId);
        return Task.FromResult(true);
    }

    public async Task<bool> DanceAsync(RoomEntityStore.RoomEntity user, int style)
    {
        if (!IsPresent(user))
            return false;

        await actions.Dance(user, style);
        return user.DanceStyle == style;
    }

    public async Task<bool> ShowSignAsync(RoomEntityStore.RoomEntity user, int sign)
    {
        if (!IsPresent(user))
            return false;

        await actions.Sign(user, sign);
        return user.Sign == sign;
    }

    public Task CollideAsync(RoomEntityStore.RoomEntity user, RoomItemStore.PlacedItem item) =>
        FireAsync(new WiredEvent(WiredEventType.Collision, user, item));

    public void ShowMessage(RoomEntityStore.RoomEntity user, string text, int style, bool toEveryone)
    {
        // about the user, and shown over them: theirs is the room index it names
        var message = WhisperMessageComposer.From(
            new ChatMessage(user.RoomIndex, text, Gesture.None, style, Links: [], TrackingId: 0), user.RoomIndex, toSpeaker: false);
        if (toEveryone)
            broadcast(message);
        else
            sendTo(user.Player.Id, message);
    }

    public void ResetTimers() => _timerStart = _pulse;

    public async Task SendSignalAsync(RoomItemStore.PlacedItem antenna, IReadOnlyList<RoomItemStore.PlacedItem> furni,
        IReadOnlyList<RoomEntityStore.RoomEntity> users, WiredRun from)
    {
        // once the pulse has run all the stacks it may, a signal would only have every stack refuse it again
        if (RunsSpent || from.Execution.Stopped)
            return;

        // the receivers run inside the sender's execution: its budgets, and its context values
        FlashItem(antenna);
        await FireAsync(new WiredEvent(WiredEventType.SignalReceived, from.User, antenna)
        {
            SignalFurni = furni.ToList(),
            SignalUsers = users.ToList(),
            Execution = from.Execution,
        });
    }
}
