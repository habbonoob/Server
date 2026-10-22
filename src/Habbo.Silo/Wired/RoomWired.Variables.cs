using Habbo.Domain.Furniture.Data;
using Habbo.Infrastructure;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired.Variables;
using Microsoft.EntityFrameworkCore;
using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Silo.Wired;

/// <summary>The room's wired variables: the internal ones, those its variable boxes create, and the
/// values furni, users, the room and each run hold. Permanent values are kept in the database.</summary>
public sealed partial class RoomWired
{
    /// <summary>Variables per diff message: a room's list arrives in chunks this big.</summary>
    private const int VariablesPerChunk = 100;

    private readonly Dictionary<(string Variable, int ItemId), WiredVariableValue> _furniValues = [];
    private readonly Dictionary<(string Variable, Guid UserId), WiredVariableValue> _userValues = [];
    private readonly Dictionary<string, WiredVariableValue> _roomValues = [];

    /// <summary>The variables other rooms share that this room's references stand for, by id.</summary>
    private readonly Dictionary<string, WiredVariable> _sharedSources = [];

    /// <summary>How often a reference to another room's room variable reads it again: every five seconds.</summary>
    private const int ReferenceRefreshPulses = 10;

    /// <summary>The room's id, which permanent values are kept under.</summary>
    public int RoomId { get; set; }

    /// <summary>The room's group, as the room grain last loaded it.</summary>
    public int? GroupId { get; set; }

    /// <summary>Every variable in the room: the internal ones, then its boxes', oldest first.</summary>
    public IReadOnlyList<WiredVariable> Variables =>
        WiredInternalVariables.All.Select(v => v.Definition)
            .Concat(_boxes.Values.Where(box => box.Type is WiredVariableBox).OrderBy(box => box.Id)
                .SelectMany(box => SubVariables(box, DefineVariable(box)).Prepend(DefineVariable(box))))
            .ToList();

    /// <summary>The sub-variables a variable box's stack gives its variable: a level system's, a time's.</summary>
    private IEnumerable<WiredVariable> SubVariables(WiredBox box, WiredVariable parent)
    {
        var stack = Stacks.GetValueOrDefault((box.Item.X, box.Item.Y)) ?? [];
        foreach (var addon in stack)
        {
            var subs = addon.Type switch
            {
                Addons.VariableLevelUpAddon => Addons.VariableLevelUpAddon.SubVariables(addon, parent),
                Addons.VariableTimeUtilAddon => Addons.VariableTimeUtilAddon.SubVariables(addon, parent, this),
                _ => [],
            };
            foreach (var sub in subs)
                yield return sub;
        }
    }

    /// <summary>The hash of the room's variables, which a client's cache is checked against.</summary>
    public int VariablesHash =>
        WiredVariable.StableHash(string.Join(",", Variables.Select(v => v.Id + ":" + v.Hash.ToString(System.Globalization.CultureInfo.InvariantCulture))));

    public WiredVariable? FindVariable(string id) =>
        WiredInternalVariables.Find(id)?.Definition
        ?? _sharedSources.GetValueOrDefault(id)
        ?? (int.TryParse(id, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var itemId)
            && _boxes.GetValueOrDefault(itemId) is { Type: WiredVariableBox } box
                ? _defining.Contains(box.Id) ? null : DefineVariable(box)
                : FindSubVariable(id));

    /// <summary>A sub-variable, by its "variable.part" id.</summary>
    private WiredVariable? FindSubVariable(string id)
    {
        var separator = id.IndexOf(WiredSubVariables.Separator);
        if (separator <= 0
            || !int.TryParse(id[..separator], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var itemId)
            || _boxes.GetValueOrDefault(itemId) is not { Type: WiredVariableBox } box
            || _defining.Contains(box.Id))
            return null;

        return SubVariables(box, DefineVariable(box)).FirstOrDefault(v => v.Id == id);
    }

    /// <summary>A sub-variable's value for a holder: what it works out from its variable's.</summary>
    private int? SubValue(WiredVariable sub, WiredHolder holder, WiredRun? run) =>
        FindVariable(sub.SubOf!) is { } parent && HasVariable(parent, holder, run)
            ? sub.Compute!(VariableValue(parent, holder, run), VariableTimes(parent, holder, run))
            : null;

    /// <summary>The variable boxes being defined right now: an echo or reference that leads back to one of
    /// them (itself, or two naming each other) finds nothing there, and so is plain instead of recursing forever.</summary>
    private readonly HashSet<int> _defining = [];

    /// <summary>The variable a box makes, with what its stack's add-ons give it: the texts of a text connector.</summary>
    private WiredVariable DefineVariable(WiredBox box)
    {
        var type = (WiredVariableBox)box.Type;
        if (!_defining.Add(box.Id))
            return type.Define(box);

        WiredVariable variable;
        try
        {
            variable = type.Define(box, FindVariable);
        }
        finally
        {
            _defining.Remove(box.Id);
        }

        var stack = Stacks.GetValueOrDefault((box.Item.X, box.Item.Y));
        if (stack?.FirstOrDefault(other => other.Type is Addons.VariableTextConnectorAddon) is { } connector)
            variable = variable with { TextConnector = Addons.VariableTextConnectorAddon.Texts(connector) };
        return variable;
    }

    /// <summary>What an echo stands for: the variable it echoes.</summary>
    private WiredVariable Resolve(WiredVariable variable) =>
        variable.EchoOf is { } source && FindVariable(source) is { } echoed ? echoed : variable;

    /// <summary>What a client holding these variables (by id and hash) lacks: the chunks of the
    /// differences, the last one flagged.</summary>
    public IReadOnlyList<WiredAllVariablesDiffsComposer> VariableDiffs(IReadOnlyDictionary<string, int> known)
    {
        var variables = Variables;
        var hash = VariablesHash;
        var ids = variables.Select(v => v.Id).ToHashSet();
        var removed = known.Keys.Where(id => !ids.Contains(id)).ToList();
        var changed = variables.Where(v => known.GetValueOrDefault(v.Id) != v.Hash || !known.ContainsKey(v.Id))
            .Select(v => new WiredVariableWithHashData(v.Hash, v.ToWire())).ToList();

        var chunks = changed.Chunk(VariablesPerChunk).ToList();
        if (chunks.Count == 0)
            return [new WiredAllVariablesDiffsComposer(hash, IsLastChunk: true, removed, [])];

        return chunks.Select((chunk, index) => new WiredAllVariablesDiffsComposer(
            hash, IsLastChunk: index == chunks.Count - 1, index == 0 ? removed : [], chunk)).ToList();
    }

    /// <summary>What a dialog knows of the room's variables: their hash, and for a variable box, who
    /// holds its variable and with what.</summary>
    private IReadOnlyList<WiredContextEntryData> DialogContext(WiredBox box)
    {
        var entries = new List<WiredContextEntryData> { WiredContextEntryData.AllVariables(VariablesHash) };
        if (box.Type is not WiredVariableBox type)
            return entries;

        var variable = DefineVariable(box);
        switch (variable.Target)
        {
            case WiredVariableTargets.Furni:
                entries.Add(WiredContextEntryData.Holding(WiredContextEntryData.FurniVariableType, variable.ToWire(),
                    _furniValues.Where(v => v.Key.Variable == variable.Id)
                        .Select(v => new WiredVariableHolderData(v.Key.ItemId, v.Value.Value)).ToList()));
                break;
            case WiredVariableTargets.User:
                entries.Add(WiredContextEntryData.Holding(WiredContextEntryData.UserVariableType, variable.ToWire(),
                    _userValues.Where(v => v.Key.Variable == variable.Id)
                        .Select(v => (User: entities.Find(v.Key.UserId), v.Value))
                        .Where(v => v.User is not null)
                        .Select(v => new WiredVariableHolderData(v.User!.RoomIndex, v.Value.Value)).ToList()));
                break;
            case WiredVariableTargets.Global:
                entries.Add(WiredContextEntryData.RoomVariable(variable.ToWire(), _roomValues.GetValueOrDefault(variable.Id)?.Value ?? 0));
                break;
        }

        return entries;
    }

    // ---------- values ----------

    /// <summary>Whether the holder may hold a value of its own: bots and pets all share one empty player id, so a user
    /// variable's values are not theirs to hold.</summary>
    private static bool CanHold(WiredHolder holder) => holder.User is null or { Bot: null, Pet: null };

    private WiredVariableValue? Stored(WiredVariable variable, WiredHolder holder, WiredRun? run) => holder.Target switch
    {
        WiredVariableTargets.Furni when holder.Item is { } item => _furniValues.GetValueOrDefault((variable.Id, item.Item.Id)),
        WiredVariableTargets.User when holder.User is { Bot: null, Pet: null } user => _userValues.GetValueOrDefault((variable.Id, user.Player.Id)),
        WiredVariableTargets.Global => _roomValues.GetValueOrDefault(variable.Id),
        WiredVariableTargets.Context => run?.ContextValues.GetValueOrDefault(variable.Id),
        _ => null,
    };

    private void Put(WiredVariable variable, WiredHolder holder, WiredRun? run, WiredVariableValue? value)
    {
        switch (holder.Target)
        {
            case WiredVariableTargets.Furni when holder.Item is { } item:
                Set(_furniValues, (variable.Id, item.Item.Id), value);
                break;
            case WiredVariableTargets.User when holder.User is { Bot: null, Pet: null } user:
                Set(_userValues, (variable.Id, user.Player.Id), value);
                break;
            case WiredVariableTargets.Global:
                Set(_roomValues, variable.Id, value);
                break;
            case WiredVariableTargets.Context when run is not null:
                Set(run.ContextValues, variable.Id, value);
                break;
        }

        static void Set<TKey>(Dictionary<TKey, WiredVariableValue> values, TKey key, WiredVariableValue? value) where TKey : notnull
        {
            if (value is null)
                values.Remove(key);
            else
                values[key] = value;
        }
    }

    public bool HasVariable(WiredVariable variable, WiredHolder holder, WiredRun? run) => HasResolved(Resolve(variable), holder, run);

    private bool HasResolved(WiredVariable variable, WiredHolder holder, WiredRun? run) =>
        variable.SubOf is not null ? holder.Target == variable.Target && SubValue(variable, holder, run) is not null
        : holder.Target == variable.Target
        && (WiredInternalVariables.Find(variable.Id) is { } internalVariable
            ? internalVariable.Read(holder, run, this) is not null
            : variable.AlwaysAvailable || Stored(variable, holder, run) is not null);

    /// <summary>The holder's value of the variable; null when it does not hold it.</summary>
    public int? VariableValue(WiredVariable variable, WiredHolder holder, WiredRun? run)
    {
        variable = Resolve(variable);
        if (holder.Target != variable.Target)
            return null;
        if (variable.SubOf is not null)
            return SubValue(variable, holder, run);
        if (WiredInternalVariables.Find(variable.Id) is { } internalVariable)
            return internalVariable.Read(holder, run, this);
        return Stored(variable, holder, run)?.Value ?? (variable.AlwaysAvailable ? 0 : null);
    }

    /// <summary>When the holder was given the variable and last changed it; null when it was not.</summary>
    public WiredVariableValue? VariableTimes(WiredVariable variable, WiredHolder holder, WiredRun? run) =>
        Resolve(variable) is var resolved && holder.Target == resolved.Target ? Stored(resolved, holder, run) : null;

    /// <summary>Gives the holder the variable, with a value; one it already holds keeps its value
    /// unless the giving overrides it. An override gives it anew, as Habbo measured: a fresh creation time, and
    /// "created" fires again rather than "changed".</summary>
    public Task<bool> GiveVariableAsync(WiredVariable variable, WiredHolder holder, WiredRun? run, int value, bool overrideExisting) =>
        GiveVariableAsync(variable, holder, run, value, overrideExisting, WiredVariableOrigins.ThisRoom);

    private async Task<bool> GiveVariableAsync(WiredVariable variable, WiredHolder holder, WiredRun? run, int value,
        bool overrideExisting, int origin)
    {
        variable = Resolve(variable);
        if (holder.Target != variable.Target || !variable.CanCreateAndDelete || !CanHold(holder) || !TakeVariableOp())
            return false;

        var existing = Stored(variable, holder, run);
        if (existing is not null && !(overrideExisting && variable.HasValue))
            return false;
        if (!MayKeep(variable, holder))
            return false;

        var stored = new WiredVariableValue(variable.HasValue ? value : 0, Clock());
        Put(variable, holder, run, stored);
        await KeepAsync(variable, holder, stored);
        await VariableChangedAsync(variable, holder, WiredVariableChange.Created, existing?.Value, stored.Value, origin);
        return true;
    }

    public Task<bool> SetVariableAsync(WiredVariable variable, WiredHolder holder, WiredRun? run, int value) =>
        SetVariableAsync(variable, holder, run, value, WiredVariableOrigins.ThisRoom);

    private async Task<bool> SetVariableAsync(WiredVariable variable, WiredHolder holder, WiredRun? run, int value, int origin)
    {
        variable = Resolve(variable);
        if (holder.Target != variable.Target || !variable.CanWriteValue || !TakeVariableOp())
            return false;
        if (WiredInternalVariables.Find(variable.Id) is { } internalVariable)
            return internalVariable.Read(holder, run, this) is not null && await internalVariable.WriteAsync(holder, value, this);

        var stored = Stored(variable, holder, run);
        if (stored is null && !variable.AlwaysAvailable)
            return false;
        if (!CanHold(holder) || !MayKeep(variable, holder))
            return false;
        if (stored is null)
        {
            stored = new WiredVariableValue(0, Clock());
            Put(variable, holder, run, stored);
        }

        var old = stored.Value;
        stored.Value = value;
        stored.Updated = Clock();
        await KeepAsync(variable, holder, stored);
        await VariableChangedAsync(variable, holder, WiredVariableChange.Changed, old, value, origin);
        return true;
    }

    public Task<bool> RemoveVariableAsync(WiredVariable variable, WiredHolder holder, WiredRun? run) =>
        RemoveVariableAsync(variable, holder, run, WiredVariableOrigins.ThisRoom);

    private async Task<bool> RemoveVariableAsync(WiredVariable variable, WiredHolder holder, WiredRun? run, int origin)
    {
        variable = Resolve(variable);
        if (holder.Target != variable.Target || !variable.CanCreateAndDelete || Stored(variable, holder, run) is not { } stored
            || !TakeVariableOp() || !MayKeep(variable, holder))
            return false;

        Put(variable, holder, run, null);
        await KeepAsync(variable, holder, null);
        await VariableChangedAsync(variable, holder, WiredVariableChange.Deleted, stored.Value, null, origin);
        return true;
    }

    /// <summary>Everyone and everything holding a furni or user variable, with their values.</summary>
    public IEnumerable<(WiredHolder Holder, int Value)> VariableHolders(WiredVariable variable)
    {
        variable = Resolve(variable);
        if (variable.SubOf is not null)
        {
            return FindVariable(variable.SubOf) is { } parent
                ? VariableHolders(parent).Select(h => (h.Holder, Value: SubValue(variable, h.Holder, null)))
                    .Where(h => h.Value is not null).Select(h => (h.Holder, h.Value!.Value)).ToList()
                : [];
        }

        if (variable.Target == WiredVariableTargets.Furni)
        {
            return WiredInternalVariables.Find(variable.Id) is { } furniVariable
                ? items.Items.Select(item => (WiredHolder.Of(item), furniVariable.Read(WiredHolder.Of(item), null, this)))
                    .Where(h => h.Item2 is not null).Select(h => (h.Item1, h.Item2!.Value)).ToList()
                : _furniValues.Where(v => v.Key.Variable == variable.Id)
                    .Select(v => (Item: items.Find(v.Key.ItemId), v.Value.Value))
                    .Where(v => v.Item is not null).Select(v => (WiredHolder.Of(v.Item!), v.Value)).ToList();
        }

        if (variable.Target == WiredVariableTargets.User)
        {
            return WiredInternalVariables.Find(variable.Id) is { } userVariable
                ? entities.Entities.Select(user => (WiredHolder.Of(user), userVariable.Read(WiredHolder.Of(user), null, this)))
                    .Where(h => h.Item2 is not null).Select(h => (h.Item1, h.Item2!.Value)).ToList()
                : _userValues.Where(v => v.Key.Variable == variable.Id)
                    .Select(v => (User: entities.Find(v.Key.UserId), v.Value.Value))
                    .Where(v => v.User is not null).Select(v => (WiredHolder.Of(v.User!), v.Value)).ToList();
        }

        return [];
    }

    /// <summary>Tells the variable triggers, for a variable whose changes may be listened to.</summary>
    private Task VariableChangedAsync(WiredVariable variable, WiredHolder holder, WiredVariableChange change, int? oldValue,
        int? newValue, int origin)
    {
        // the displays show every change, whether or not wired may listen to it
        if (holder.Target is WiredVariableTargets.User or WiredVariableTargets.Furni)
            DisplaysHear(variable, holder, change, newValue);

        return variable.CanInterceptChanges && holder.Target != WiredVariableTargets.Context
            ? FireAsync(new WiredEvent(WiredEventType.VariableChanged, holder.User, holder.Item)
            {
                VariableId = variable.Id,
                VariableChange = change,
                OldValue = oldValue,
                NewValue = newValue,
                VariableOrigin = origin,
            })
            : Task.CompletedTask;
    }

    // ---------- the wired menu ----------

    /// <summary>Every variable the holder holds, with its value (0 for a variable with none).</summary>
    public IReadOnlyList<(string Id, int Value)> HeldVariables(WiredHolder holder) =>
        Variables.Where(v => v.Target == holder.Target && HasVariable(v, holder, null))
            .Select(v => (v.Id, VariableValue(v, holder, null) ?? 0)).ToList();

    /// <summary>The wired boxes whose picks include the item.</summary>
    public IReadOnlyList<int> BoxesPicking(int itemId) =>
        _boxes.Values.Where(box => box.Settings.StuffIds.Contains(itemId) || box.Settings.StuffIds2.Contains(itemId))
            .Select(box => box.Id).ToList();

    /// <summary>The holder the menu means: a furni by item id, a user by room index, or the room.</summary>
    public WiredHolder? HolderOf(int target, int objectId) => target switch
    {
        WiredVariableTargets.Furni => items.Find(objectId) is { } item ? WiredHolder.Of(item) : null,
        WiredVariableTargets.User => entities.FindByIndex(objectId) is { } user ? WiredHolder.Of(user) : null,
        WiredVariableTargets.Global => WiredHolder.Room,
        _ => null,
    };

    /// <summary>What the menu does to a held variable: sets it, gives it (over what is there), or takes it.</summary>
    public Task<bool> EditVariableAsync(WiredVariable variable, WiredHolder holder, int value, int action) => action switch
    {
        WiredVariableMenuActions.Set => SetVariableAsync(variable, holder, null, value, WiredVariableOrigins.Inspection),
        WiredVariableMenuActions.Give => GiveVariableAsync(variable, holder, null, value, overrideExisting: true, WiredVariableOrigins.Inspection),
        WiredVariableMenuActions.Remove => RemoveVariableAsync(variable, holder, null, WiredVariableOrigins.Inspection),
        _ => Task.FromResult(false),
    };

    // ---------- keeping permanent values ----------

    private static (int? ItemId, Guid? UserId) Keys(WiredHolder holder) =>
        (holder.Item?.Item.Id, holder.User?.Player.Id);

    /// <summary>Where a variable's values are kept: this room, under its own id — or, for a
    /// reference, the room it stands for, under the id there.</summary>
    private (int Room, string Id) KeptAs(WiredVariable variable) =>
        (variable.SourceRoomId ?? RoomId, variable.SourceId ?? variable.Id);

    private static bool IsKept(WiredVariable variable) =>
        WiredVariableAvailability.IsPermanent(variable.Availability) || variable.SourceRoomId is not null;

    /// <summary>Whether the holder's value of the variable is written to the database when it changes.</summary>
    private bool Keeps(WiredVariable variable, WiredHolder holder) =>
        RoomId != 0 && IsKept(variable) && holder.Target != WiredVariableTargets.Context && holder.Item is not { Temporary: true };

    /// <summary>Whether the pulse may make a change to the holder's value: one that is kept spends a write, asked before
    /// anything changes, so past the budget the change is refused rather than held but never kept.</summary>
    private bool MayKeep(WiredVariable variable, WiredHolder holder) => !Keeps(variable, holder) || TakeWrite();

    /// <summary>Writes a permanent variable's value for the holder (or its removal) to the database;
    /// for a reference, in the room it stands for, which is told. Its write was taken with <see cref="MayKeep"/>.</summary>
    private async Task KeepAsync(WiredVariable variable, WiredHolder holder, WiredVariableValue? value)
    {
        if (!Keeps(variable, holder))
            return;

        var (itemId, userId) = Keys(holder);
        var (keptRoom, keptId) = KeptAs(variable);
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.WiredVariableValues.FirstOrDefaultAsync(r =>
            r.RoomId == keptRoom && r.VariableId == keptId && r.ItemId == itemId && r.UserId == userId);
        if (value is null)
        {
            if (row is not null)
                db.WiredVariableValues.Remove(row);
        }
        else if (row is null)
        {
            db.WiredVariableValues.Add(new WiredVariableValueRow
            {
                RoomId = keptRoom, VariableId = keptId, ItemId = itemId, UserId = userId,
                Value = value.Value, CreatedAt = value.Created, UpdatedAt = value.Updated,
            });
        }
        else
        {
            row.Value = value.Value;
            row.UpdatedAt = value.Updated;
        }

        await db.SaveChangesAsync();

        if (variable.SourceId is not null && keptRoom != RoomId)
        {
            await actions.SharedChanged(new SharedVariableChange(keptRoom, keptId, userId, value?.Value,
                value?.Created ?? Clock(), value?.Updated ?? Clock()));
        }
    }

    // ---------- shared variables and references ----------

    /// <summary>The variable of a variable box kept anywhere, if it is one this room may reference:
    /// kept and shared across rooms.</summary>
    private WiredVariable? SharedVariable(int itemId, int roomId, string className, FurniWired settings) =>
        registry.For(className) is WiredVariableBox type
        && type.Define(itemId, settings) is { Availability: WiredVariableAvailability.PermanentShared } variable
            ? variable with { SourceRoomId = roomId }
            : null;

    /// <summary>The variables the owner's other rooms share, which this room's references may stand for.</summary>
    public async Task<IReadOnlyList<(int RoomId, string RoomName, WiredVariable Variable)>> SharedVariablesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var boxes = await db.FurniWiredBoxes.AsNoTracking()
            .Join(db.UserItems, w => w.ItemId, i => i.Id, (w, i) => new { Wired = w, i.RoomId, i.Definition!.ClassName })
            .Join(db.Rooms, x => x.RoomId, r => r.Id, (x, r) => new { x.Wired, x.ClassName, RoomId = r.Id, RoomName = r.Name, r.OwnerId })
            .Where(x => x.OwnerId == ownerId && x.RoomId != RoomId)
            .ToListAsync();

        return boxes
            .Select(x => (x.RoomId, x.RoomName, Variable: SharedVariable(x.Wired.ItemId, x.RoomId, x.ClassName, x.Wired)))
            .Where(x => x.Variable is not null)
            .Select(x => (x.RoomId, x.RoomName, x.Variable!))
            .ToList();
    }

    /// <summary>Learns what the room's references stand for: shared variables in the owner's other rooms, and no one
    /// else's (a reference names its source by item id, which the client could set to anyone's).</summary>
    private async Task LoadSharedSourcesAsync()
    {
        var ids = _boxes.Values.Where(box => box.Type is ReferenceVariableBox)
            .Select(box => box.Settings.VariableIds.FirstOrDefault())
            .Where(id => int.TryParse(id, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _))
            .Select(id => int.Parse(id!, System.Globalization.CultureInfo.InvariantCulture))
            .ToList();
        _sharedSources.Clear();
        if (ids.Count == 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var boxes = await db.FurniWiredBoxes.AsNoTracking()
            .Where(w => ids.Contains(w.ItemId))
            .Join(db.UserItems, w => w.ItemId, i => i.Id, (w, i) => new { Wired = w, i.RoomId, i.Definition!.ClassName })
            .Join(db.Rooms, x => x.RoomId, r => r.Id, (x, r) => new { x.Wired, x.RoomId, x.ClassName, r.OwnerId })
            .Where(x => x.OwnerId == ownerId)
            .ToListAsync();
        foreach (var box in boxes.Where(b => b.RoomId is { } roomId && roomId != RoomId))
        {
            if (SharedVariable(box.Wired.ItemId, box.RoomId!.Value, box.ClassName, box.Wired) is { } variable)
                _sharedSources[variable.Id] = variable;
        }
    }

    /// <summary>The room's references to room variables read their values again.</summary>
    private async Task RefreshReferencesAsync()
    {
        var references = Variables.Where(v => v.SourceRoomId is not null && v.Target == WiredVariableTargets.Global).ToList();
        if (references.Count == 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        foreach (var reference in references)
        {
            var (keptRoom, keptId) = KeptAs(reference);
            var row = await db.WiredVariableValues.AsNoTracking().FirstOrDefaultAsync(r =>
                r.RoomId == keptRoom && r.VariableId == keptId && r.ItemId == null && r.UserId == null);
            if (row is null)
                _roomValues.Remove(reference.Id);
            else
                _roomValues[reference.Id] = new WiredVariableValue(row.Value, row.CreatedAt) { Updated = row.UpdatedAt };
        }
    }

    /// <summary>A room referencing one of this room's shared variables changed it: this room holds the
    /// new value (already kept) and its triggers hear of it.</summary>
    public async Task SharedVariableChangedAsync(SharedVariableChange change)
    {
        if (FindVariable(change.VariableId) is not { } variable)
            return;

        WiredHolder? holder = change.UserId is { } userId
            ? entities.Find(userId) is { } user ? WiredHolder.Of(user) : null
            : WiredHolder.Room;
        if (holder is not { } h)
            return;

        var old = Stored(variable, h, null);
        var value = change.Value is { } v ? new WiredVariableValue(v, change.CreatedAt) { Updated = change.UpdatedAt } : null;
        Put(variable, h, null, value);
        var kind = value is null ? WiredVariableChange.Deleted : old is null ? WiredVariableChange.Created : WiredVariableChange.Changed;
        await VariableChangedAsync(variable, h, kind, old?.Value, value?.Value, WiredVariableOrigins.OtherRoom);
    }

    /// <summary>What a reference box's dialog knows beyond others': the variables it may stand for.</summary>
    public async Task<IComposer?> DialogForAsync(int itemId)
    {
        if (_boxes.GetValueOrDefault(itemId) is not { } box)
            return null;

        var context = DialogContext(box).ToList();
        if (box.Type is ReferenceVariableBox)
        {
            context.Add(WiredContextEntryData.Shared((await SharedVariablesAsync())
                .Select(s => new WiredSharedVariableData(s.RoomId, s.RoomName, s.Variable.ToWire())).ToList()));
        }

        return WiredDialogs.For(box, items, context);
    }

    private IReadOnlyList<string> PermanentVariableIds(int target) =>
        Variables.Where(v => v.Kind == WiredVariableKinds.CreatedByUser && v.Target == target
                             && WiredVariableAvailability.IsPermanent(v.Availability))
            .Select(v => v.Id).ToList();

    /// <summary>Loads the room's kept furni and room values, once its boxes are in.</summary>
    public async Task LoadVariablesAsync(AppDbContext db, CancellationToken ct = default)
    {
        await LoadSharedSourcesAsync();
        await RefreshReferencesAsync();

        var furni = PermanentVariableIds(WiredVariableTargets.Furni);
        var room = PermanentVariableIds(WiredVariableTargets.Global);
        var ids = furni.Concat(room).ToList();
        var rows = await db.WiredVariableValues.AsNoTracking()
            .Where(r => r.RoomId == RoomId && r.UserId == null && ids.Contains(r.VariableId))
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            var value = new WiredVariableValue(row.Value, row.CreatedAt) { Updated = row.UpdatedAt };
            if (row.ItemId is { } itemId)
            {
                if (items.Find(itemId) is not null)
                    _furniValues[(row.VariableId, itemId)] = value;
            }
            else
            {
                _roomValues[row.VariableId] = value;
            }
        }
    }

    /// <summary>A user came in: their kept values of the room's user variables come back.</summary>
    public async Task LoadUserVariablesAsync(RoomEntityStore.RoomEntity user)
    {
        var ids = PermanentVariableIds(WiredVariableTargets.User);
        if (RoomId == 0 || !CanHold(WiredHolder.Of(user)))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var rows = await db.WiredVariableValues.AsNoTracking()
            .Where(r => r.RoomId == RoomId && r.UserId == user.Player.Id && ids.Contains(r.VariableId))
            .ToListAsync();
        foreach (var row in rows)
            _userValues[(row.VariableId, user.Player.Id)] = new WiredVariableValue(row.Value, row.CreatedAt) { Updated = row.UpdatedAt };

        // and their values of the user variables other rooms share with this one
        await LoadReferencedUserValuesAsync(db, user);
    }

    /// <summary>A reference box was put down or saved: it learns what it stands for, and the users here their values of
    /// it.</summary>
    private async Task ReferencePlacedAsync()
    {
        await LoadSharedSourcesAsync();
        await RefreshReferencesAsync();
        if (RoomId == 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        foreach (var user in entities.Entities.Where(user => CanHold(WiredHolder.Of(user))).ToList())
            await LoadReferencedUserValuesAsync(db, user);
    }

    /// <summary>The user's values of the user variables other rooms share with this one.</summary>
    private async Task LoadReferencedUserValuesAsync(AppDbContext db, RoomEntityStore.RoomEntity user)
    {
        foreach (var reference in Variables.Where(v => v.SourceRoomId is not null && v.Target == WiredVariableTargets.User))
        {
            var (keptRoom, keptId) = KeptAs(reference);
            var row = await db.WiredVariableValues.AsNoTracking().FirstOrDefaultAsync(r =>
                r.RoomId == keptRoom && r.VariableId == keptId && r.UserId == user.Player.Id);
            if (row is not null)
                _userValues[(reference.Id, user.Player.Id)] = new WiredVariableValue(row.Value, row.CreatedAt) { Updated = row.UpdatedAt };
        }
    }

    /// <summary>A user left: what they held lives on only where it is kept.</summary>
    private void ForgetUser(RoomEntityStore.RoomEntity user)
    {
        foreach (var key in _userValues.Keys.Where(key => key.UserId == user.Player.Id).ToList())
            _userValues.Remove(key);
    }

    /// <summary>An item left the room: what it held lives on only where it is kept.</summary>
    private void ForgetItem(int itemId)
    {
        foreach (var key in _furniValues.Keys.Where(key => key.ItemId == itemId).ToList())
            _furniValues.Remove(key);
    }

    /// <summary>An item came into the room: its kept values come back.</summary>
    private async Task LoadItemVariablesAsync(AppDbContext db, RoomItemStore.PlacedItem item)
    {
        var ids = PermanentVariableIds(WiredVariableTargets.Furni);
        if (ids.Count == 0 || RoomId == 0)
            return;

        var rows = await db.WiredVariableValues.AsNoTracking()
            .Where(r => r.RoomId == RoomId && r.ItemId == item.Item.Id && ids.Contains(r.VariableId))
            .ToListAsync();
        foreach (var row in rows)
            _furniValues[(row.VariableId, item.Item.Id)] = new WiredVariableValue(row.Value, row.CreatedAt) { Updated = row.UpdatedAt };
    }

    /// <summary>A variable is gone, or changed how long its values last: every value it had goes,
    /// kept ones too.</summary>
    private async Task ClearVariableAsync(string variableId)
    {
        foreach (var key in _furniValues.Keys.Where(key => key.Variable == variableId).ToList())
            _furniValues.Remove(key);
        foreach (var key in _userValues.Keys.Where(key => key.Variable == variableId).ToList())
            _userValues.Remove(key);
        _roomValues.Remove(variableId);

        if (RoomId == 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        await db.WiredVariableValues.Where(r => r.RoomId == RoomId && r.VariableId == variableId).ExecuteDeleteAsync();
    }

    /// <summary>A variable box left the room: its variable's values go, and every box that used it
    /// forgets it.</summary>
    private async Task VariableRemovedAsync(string variableId)
    {
        await ClearVariableAsync(variableId);
        foreach (var box in _boxes.Values.Where(box => box.Settings.VariableIds.Contains(variableId)).ToList())
        {
            box.Settings.VariableIds = box.Settings.VariableIds.Select(id => id == variableId ? string.Empty : id).ToList();
            await StoreAsync(box.Settings);
        }
    }
}
