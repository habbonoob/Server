using Habbo.Silo.RewardTracks;

namespace Habbo.Silo.Tests;

/// <summary>Daily tasks as habbo.com sets them: three a day, ending at midnight UTC (the time left it sent ran out at
/// 23:59:59 UTC each time, October 2026).</summary>
public class DailyTaskRulesTests
{
    private static DailyTaskDefinition Task(string code, string action) => new(code, action, null, 1, [new(RewardTrackPrizeTypes.Currency, "0", Amount: 10)]);

    private static readonly List<DailyTaskDefinition> Pool =
    [
        Task("a", RewardTrackActions.Dance), Task("b", RewardTrackActions.Dance), Task("c", RewardTrackActions.Wave),
        Task("d", RewardTrackActions.Swim), Task("e", RewardTrackActions.Sit), Task("f", RewardTrackActions.Dance),
    ];

    [Fact]
    public void A_day_has_three_tasks_each_counting_something_else()
    {
        var drawn = DailyTaskRules.Draw(Pool, Guid.NewGuid(), new DateOnly(2026, 10, 25));

        Assert.Equal(DailyTaskRules.TasksPerDay, drawn.Count);
        Assert.Equal(drawn.Count, drawn.Select(t => t.Action).Distinct().Count());
    }

    [Fact]
    public void The_day_ends_at_midnight_utc()
    {
        // habbo.com, 25 October 2026: 55955 seconds left at 08:27:24.890 UTC
        var now = new DateTimeOffset(2026, 10, 25, 8, 27, 24, 890, TimeSpan.Zero);

        Assert.Equal(55955, DailyTaskRules.SecondsLeft(now));
        Assert.Equal(new DateOnly(2026, 10, 25), DailyTaskRules.Today(now.ToOffset(TimeSpan.FromHours(1))));
    }

    [Fact]
    public void A_task_with_a_parameter_counts_only_that()
    {
        var castle = new DailyTaskDefinition("castle", RewardTrackActions.FindStuff, ["sand_cstl_twr", "sand_cstl_wall"], 1, []);

        Assert.True(castle.Counts(RewardTrackActions.FindStuff, "sand_cstl_wall", 0));
        Assert.False(castle.Counts(RewardTrackActions.FindStuff, "bw_bball", 0));
    }

    [Fact]
    public void A_task_done_in_an_effect_counts_only_in_it()
    {
        // Boulevard Skating: concrete patches, on a skateboard
        var skating = new DailyTaskDefinition("skate", RewardTrackActions.WalkOverStuff, ["sb_tile"], 40, [], Effects: [71, 72]);

        Assert.True(skating.Counts(RewardTrackActions.WalkOverStuff, "sb_tile", 72));
        Assert.False(skating.Counts(RewardTrackActions.WalkOverStuff, "sb_tile", 0));
    }
}
