namespace Habbo.Silo.Wired;

/// <summary>The kinds of error the wired menu's monitor counts, by the client's numbers
/// (wiredmenu.error_info.N explains each), with the name and category it lists them under.</summary>
public static class WiredErrors
{
    public const string ErrorCategory = "ERROR";
    public const string WarningCategory = "WARNING";

    // as Habbo names and numbers them (both of the last two are 8 there, and here)
    public static readonly WiredErrorKind ExecutionLimit = new(0, "EXECUTION_CAP", ErrorCategory);
    public static readonly WiredErrorKind DelayedEventLimit = new(1, "DELAYED_EVENTS_CAP", ErrorCategory);
    public static readonly WiredErrorKind ExecutorOverload = new(2, "EXECUTOR_OVERLOAD", ErrorCategory);
    public static readonly WiredErrorKind Heavy = new(3, "MARKED_AS_HEAVY", WarningCategory);
    public static readonly WiredErrorKind Killed = new(4, "KILLED", ErrorCategory);
    public static readonly WiredErrorKind Recursion = new(5, "RECURSION_TIMEOUT", ErrorCategory);
    public static readonly WiredErrorKind TooManyVariables = new(6, "TOO_MANY_VARIABLES", ErrorCategory);
    public static readonly WiredErrorKind TransactionFailure = new(7, "TRANSACTION_FAILURE", ErrorCategory);
    public static readonly WiredErrorKind PlacementFailure = new(8, "PLACEMENT_FAILURE", ErrorCategory);
    public static readonly WiredErrorKind VariableEffectLimit = new(8, "VARIABLE_FX_LIMIT", ErrorCategory);

    /// <summary>Every kind, in the order the monitor lists them: all of them, happened or not.</summary>
    public static readonly IReadOnlyList<WiredErrorKind> All =
    [
        ExecutionLimit, DelayedEventLimit, ExecutorOverload, Heavy, Killed, Recursion, TooManyVariables,
        TransactionFailure, PlacementFailure, VariableEffectLimit,
    ];

    /// <summary>What the monitor is told of an error that has not happened: no time since.</summary>
    public const long NeverHappened = -1;
}

public sealed record WiredErrorKind(int Id, string Name, string Category);

/// <summary>How often an error happened in the room, and when last.</summary>
public sealed class WiredErrorCount(WiredErrorKind kind)
{
    public WiredErrorKind Kind { get; } = kind;
    public int Count { get; set; }
    public DateTimeOffset Last { get; set; }
}

/// <summary>The wired log's levels and sources (wiredmenu.logs_overview.log_level.N / log_source.N).</summary>
public static class WiredLogLevels
{
    public const byte Info = 0;
    public const byte Warn = 1;
    public const byte Error = 2;
    public const byte Debug = 3;

    public const byte SystemSource = 0;
    public const byte WiredSource = 1;
}
