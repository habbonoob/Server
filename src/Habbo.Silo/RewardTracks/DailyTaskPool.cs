using Habbo.Infrastructure;

namespace Habbo.Silo.RewardTracks;

/// <summary>The daily tasks the hotel sets, read from seed/daily_tasks.json once.</summary>
public sealed class DailyTaskPool(SeedFiles seedFiles)
{
    private const string SeedFile = "daily_tasks.json";

    private IReadOnlyList<DailyTaskDefinition>? _tasks;
    private readonly SemaphoreSlim _loading = new(1, 1);

    public async Task<IReadOnlyList<DailyTaskDefinition>> AllAsync()
    {
        if (_tasks is not null)
            return _tasks;

        await _loading.WaitAsync();
        try
        {
            return _tasks ??= (await seedFiles.ReadJsonAsync<DailyTaskFile>(SeedFile))?.Tasks ?? [];
        }
        finally
        {
            _loading.Release();
        }
    }

    private sealed record DailyTaskFile(List<DailyTaskDefinition> Tasks);
}

/// <summary>A daily task of the pool: its code (keying its texts and image), what it counts (one of
/// <see cref="RewardTrackActions"/>, narrowed by its parameters when it has any: the furni classes, pet types or hand
/// items it means), the effects one of which the player must wear doing it (a skateboard to skate), the times it takes,
/// and its rewards.</summary>
public sealed record DailyTaskDefinition(string Code, string Action, List<string>? Parameters, int Repeats, List<ProductPrize> Rewards,
    List<int>? Effects = null)
{
    public bool Counts(string action, string parameter, int effectId) =>
        Action == action
        && (Parameters is not { Count: > 0 } || Parameters.Contains(parameter))
        && (Effects is not { Count: > 0 } || Effects.Contains(effectId));
}

/// <summary>How a day's tasks are set: three of the pool, each counting something different, drawn when the player first
/// asks for the day's and kept for it.</summary>
public static class DailyTaskRules
{
    public const int TasksPerDay = 3;

    public static List<DailyTaskDefinition> Draw(IReadOnlyList<DailyTaskDefinition> pool, Guid playerId, DateOnly day)
    {
        var random = new Random(HashCode.Combine(playerId, day.DayNumber));
        var drawn = new List<DailyTaskDefinition>();
        foreach (var task in pool.OrderBy(_ => random.Next()))
        {
            if (drawn.Count == TasksPerDay)
                break;
            if (drawn.All(d => d.Action != task.Action))
                drawn.Add(task);
        }

        return drawn;
    }

    /// <summary>Today, as daily tasks count days: in UTC.</summary>
    public static DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.UtcDateTime);

    /// <summary>The seconds until the day's tasks end, at midnight UTC.</summary>
    public static int SecondsLeft(DateTimeOffset now) =>
        (int)(now.UtcDateTime.Date.AddDays(1) - now.UtcDateTime).TotalSeconds;
}
