using Habbo.Infrastructure;

namespace Habbo.Silo.RewardTracks;

/// <summary>The hotel's reward tracks, read from seed/reward_tracks.json once: Habbo's own, as habbo.com sent them.</summary>
public sealed class RewardTrackCatalog(SeedFiles seedFiles)
{
    private const string SeedFile = "reward_tracks.json";

    private IReadOnlyList<RewardTrack>? _tracks;
    private readonly SemaphoreSlim _loading = new(1, 1);

    public async Task<IReadOnlyList<RewardTrack>> AllAsync()
    {
        if (_tracks is not null)
            return _tracks;

        await _loading.WaitAsync();
        try
        {
            return _tracks ??= (await seedFiles.ReadJsonAsync<RewardTrackFile>(SeedFile))?.Tracks ?? [];
        }
        finally
        {
            _loading.Release();
        }
    }

    public async Task<RewardTrack?> FindAsync(string trackId) => (await AllAsync()).FirstOrDefault(t => t.Id == trackId);

    private sealed record RewardTrackFile(List<RewardTrack> Tracks);
}

/// <summary>A reward track: its id and colour theme, what its premium side costs and gives (none when it sells
/// none), the tasks that earn its points and the prizes the points unlock.</summary>
public sealed record RewardTrack(string Id, string Theme, RewardTrackPremium? Premium, List<RewardTrackTask> Tasks, List<RewardTrackPrize> Prizes);

/// <summary>A track's premium side: task points multiplied by the boost from when it is bought, points given at
/// once, and its price.</summary>
public sealed record RewardTrackPremium(double TaskPointsBoost, int InstantPoints, int CostCredits, int CostDiamonds);

/// <summary>A task: what it counts (one of <see cref="RewardTrackActions"/>, and a parameter narrowing it when not
/// empty), whether only premium counts it, and the levels its count reaches.</summary>
public sealed record RewardTrackTask(string Id, string Action, string Parameter, bool Premium, List<RewardTrackTaskLevel> Levels)
{
    /// <summary>The count the task stops at: its last level's.</summary>
    public int MaxCount => Levels.Count == 0 ? 0 : Levels.Max(l => l.Count);
}

/// <summary>A task's level: the count that reaches it and the points it gives; a premium one only to premium.</summary>
public sealed record RewardTrackTaskLevel(int Count, int Points, bool Premium);

/// <summary>A prize: the points it unlocks at, and what it is: a client product type (<see cref="RewardTrackPrizeTypes"/>),
/// the item (a class id, a badge code, a currency type, a pet type, a Habbicon id), its extra parameters (a figure)
/// and how many; a premium one only for premium.</summary>
public sealed record RewardTrackPrize(string Id, int Points, int Type, string Item, string Extra, int Amount, bool Premium);

/// <summary>Something handed over as a prize or a reward: a client product type (<see cref="RewardTrackPrizeTypes"/>), the
/// item, its extra parameters and how many.</summary>
public sealed record ProductPrize(int Type, string Item, string Extra = "", int Amount = 1);

/// <summary>What a prize is, by the client's product type ids (ProductIconWidget).</summary>
public static class RewardTrackPrizeTypes
{
    public const int WallFurni = 0;
    public const int FloorFurni = 1;
    public const int Effect = 2;
    public const int Badge = 4;
    public const int Bot = 6;
    public const int Currency = 8;
    public const int ChatStyle = 9;
    public const int Pet = 10;
    public const int Habbicon = 12;
}
