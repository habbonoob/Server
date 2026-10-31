namespace Habbo.Silo.Wired.Variables;

/// <summary>A variable the room keeps for everything of its kind, read from (and some written
/// to) what it describes: a furni's position, a user's effect, the room's user count.</summary>
public sealed class WiredInternalVariable(WiredVariable definition, WiredInternalAccess access)
{
    public WiredVariable Definition { get; } = definition;

    /// <summary>Whether this server reads it at all; one it does not is listed, and no one holds it.</summary>
    public bool IsRead => access.Read is not null;

    /// <summary>Whether this server writes it: Habbo lets it be written, and the server has a way to.</summary>
    public bool IsWritten => Definition.CanWriteValue && access.Write is not null;

    /// <summary>The value for the holder; null when the holder does not have it. A variable with no value (a flag)
    /// reads 0 for a holder that has it.</summary>
    public int? Read(WiredHolder holder, WiredRun? run, IWiredRoom room) =>
        holder.Target == Definition.Target && access.Read is { } read ? read(holder, run, room) : null;

    public Task<bool> WriteAsync(WiredHolder holder, int value, IWiredRoom room) =>
        IsWritten ? access.Write!(holder, value, room) : Task.FromResult(false);

    /// <summary>Gives the holder the variable (a hand item, an area hider's option), where Habbo lets it be given.</summary>
    public Task<bool> GiveAsync(WiredHolder holder, int value, IWiredRoom room) =>
        Definition.CanCreateAndDelete && access.Give is { } give ? give(holder, value, room) : Task.FromResult(false);

    public Task<bool> RemoveAsync(WiredHolder holder, IWiredRoom room) =>
        Definition.CanCreateAndDelete && access.Remove is { } remove ? remove(holder, room) : Task.FromResult(false);

    /// <summary>When the holder was given it and it last changed, where Habbo lets that be read; null otherwise.</summary>
    public WiredVariableValue? Times(WiredHolder holder, WiredRun? run, IWiredRoom room) =>
        (Definition.CanReadCreationTime || Definition.CanReadLastUpdateTime) && holder.Target == Definition.Target
        && access.Times is { } times ? times(holder, run, room) : null;
}

/// <summary>How the server reads, writes, gives and takes one internal variable; any of them may be missing.</summary>
public sealed record WiredInternalAccess(
    Func<WiredHolder, WiredRun?, IWiredRoom, int?>? Read,
    Func<WiredHolder, int, IWiredRoom, Task<bool>>? Write = null,
    Func<WiredHolder, int, IWiredRoom, Task<bool>>? Give = null,
    Func<WiredHolder, IWiredRoom, Task<bool>>? Remove = null,
    Func<WiredHolder, WiredRun?, IWiredRoom, WiredVariableValue?>? Times = null)
{
    public static readonly WiredInternalAccess None = new((Func<WiredHolder, WiredRun?, IWiredRoom, int?>?)null);
}

/// <summary>The internal and smart variables (the client's "internal" tab), exactly as habbo.com defines them
/// (<see cref="WiredInternalVariableCatalog"/>), each with what this server reads and writes of it
/// (<see cref="WiredInternalVariableAccess"/>).</summary>
public sealed class WiredInternalVariables
{
    private readonly Dictionary<string, WiredInternalVariable> _byId;
    private readonly Dictionary<(int Target, string Name), WiredInternalVariable> _byName;

    private WiredInternalVariables(IReadOnlyList<WiredInternalVariable> all)
    {
        All = all;
        _byId = all.ToDictionary(v => v.Definition.Id);
        _byName = all.GroupBy(v => (v.Definition.Target, v.Definition.Name)).ToDictionary(g => g.Key, g => g.First());
    }

    public static WiredInternalVariables From(IEnumerable<WiredVariable> definitions) =>
        new(definitions.Select(d => new WiredInternalVariable(d, WiredInternalVariableAccess.For(d.Target, d.Name))).ToList());

    /// <summary>In Habbo's order, as the client lists them.</summary>
    public IReadOnlyList<WiredInternalVariable> All { get; }

    /// <summary>By Habbo's id.</summary>
    public WiredInternalVariable? Find(string id) => _byId.GetValueOrDefault(id);

    /// <summary>The variable of this name held by this target.</summary>
    public WiredInternalVariable? Named(int target, string name) => _byName.GetValueOrDefault((target, name));
}
