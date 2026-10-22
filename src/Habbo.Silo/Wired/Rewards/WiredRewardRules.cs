namespace Habbo.Silo.Wired.Rewards;

/// <summary>How a give reward box rewards: how often a user may be rewarded (once, or once every so many days, hours
/// or minutes), whether each reward goes to each user once instead of by chance, how many it gives in all (0: no
/// limit), and its rewards.</summary>
public sealed record WiredRewardRules(int Interval, int Every, bool Unique, int Limit, IReadOnlyList<WiredReward> Rewards)
{
    public const int Once = 0;
    public const int Days = 1;
    public const int Hours = 2;
    public const int Minutes = 3;

    /// <summary>How long a user waits between rewards; none when they are rewarded once.</summary>
    public TimeSpan? Wait => Interval switch
    {
        Days => TimeSpan.FromDays(Every),
        Hours => TimeSpan.FromHours(Every),
        Minutes => TimeSpan.FromMinutes(Every),
        _ => null,
    };
}
