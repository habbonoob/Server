namespace Habbo.Silo.Wired;

/// <summary>One wired execution, as Habbo budgets them: a stack set off by something that happened, and every stack
/// its signals and calls reach. Each has its own budgets, so two executions in the same pulse do not share one, and a
/// delayed effect starts afresh with the same context values. What a budget refuses is dropped without a word, as on
/// Habbo, but the room's error list counts it.</summary>
public sealed class WiredExecution
{
    /// <summary>The most times one execution runs a stack; the 101st visit is dropped (Habbo: rings of 2, 12 and 24
    /// stacks stopped at 200, 1200 and 2400 visits).</summary>
    public const int MaxVisitsPerStack = 100;

    /// <summary>The most furni one execution moves; @position and @altitude writes count as moves.</summary>
    public const int MaxMoves = 80;

    /// <summary>About as many variable operations as one execution makes before Habbo stops it.</summary>
    public const int MaxVariableOps = 10_000;

    private readonly Dictionary<(int X, int Y), int> _visits = [];
    private int _moves;
    private int _variableOps;

    public WiredExecution() : this([])
    {
    }

    private WiredExecution(Dictionary<string, Variables.WiredVariableValue> contextValues) => ContextValues = contextValues;

    /// <summary>The context variables' values: readable in every stack the execution reaches.</summary>
    public Dictionary<string, Variables.WiredVariableValue> ContextValues { get; }

    /// <summary>What the execution's stacks set off and has yet to do: stacks that heard a signal and decided, and
    /// stacks called, in the order they were.</summary>
    public Queue<Func<Task>> Pending { get; } = [];

    /// <summary>Whether the execution was stopped for making too many variable operations.</summary>
    public bool Stopped { get; private set; }

    /// <summary>A new execution carrying on this one's context values, for an effect that waited.</summary>
    public WiredExecution Continued() => new(ContextValues);

    /// <summary>Whether the stack on that tile may run once more. <paramref name="first"/> tells the first visit
    /// refused, so the room counts the error once.</summary>
    public bool TakeVisit((int X, int Y) stack, out bool first)
    {
        var visits = _visits.GetValueOrDefault(stack) + 1;
        _visits[stack] = visits;
        first = visits == MaxVisitsPerStack + 1;
        return visits <= MaxVisitsPerStack;
    }

    /// <summary>Whether one more furni may move. <paramref name="first"/> tells the first one refused, so the room
    /// counts the error once.</summary>
    public bool TakeMove(out bool first)
    {
        _moves++;
        first = _moves == MaxMoves + 1;
        return _moves <= MaxMoves;
    }

    /// <summary>Whether one more variable operation may happen; the one past the budget stops the execution, and
    /// <paramref name="first"/> tells it is that one.</summary>
    public bool TakeVariableOp(out bool first)
    {
        first = false;
        if (Stopped)
            return false;
        if (++_variableOps <= MaxVariableOps)
            return true;
        Stopped = first = true;
        return false;
    }
}
