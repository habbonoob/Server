using Habbo.Domain.RewardTracks;
using Habbo.Silo.RewardTracks;

namespace Habbo.Silo.Tests;

/// <summary>Reward track progress as habbo.com keeps it (its introduction track, 25 October 2026): a task counts up to
/// its last level, each level passed gives its points, boosted for premium, and the points unlock prizes.</summary>
public class RewardTrackRulesTests
{
    // the introduction track's "visit rooms": 10 points at 1, 20 at 5, 30 at 20
    private static readonly RewardTrackTask VisitRooms = new("visit_rooms", RewardTrackActions.EnterOtherUsersRoom, "", false,
        [new(1, 10, false), new(5, 20, false), new(20, 30, false)]);

    private static readonly RewardTrack Introduction = new("introduction", "blue", new RewardTrackPremium(1.2, 0, 100, 100),
        [VisitRooms], [new("free_001", 50, RewardTrackPrizeTypes.Habbicon, "28", "", 1, false), new("premium_001", 50, RewardTrackPrizeTypes.Habbicon, "50", "", 1, true)]);

    private static RewardTrackProgress Fresh() => new() { TrackId = Introduction.Id };

    [Fact]
    public void Each_level_passed_gives_its_points()
    {
        var progress = Fresh();

        var counted = RewardTrackRules.Count(Introduction, progress, RewardTrackActions.EnterOtherUsersRoom, "", 5);

        Assert.Equal(5, Assert.Single(counted).Count);
        Assert.Equal(30, progress.Points);
    }

    [Fact]
    public void A_task_stops_counting_at_its_last_level()
    {
        var progress = Fresh();
        RewardTrackRules.Count(Introduction, progress, RewardTrackActions.EnterOtherUsersRoom, "", 25);

        Assert.Equal(20, RewardTrackRules.CountOf(progress, VisitRooms));
        Assert.Equal(60, progress.Points);
        Assert.Empty(RewardTrackRules.Count(Introduction, progress, RewardTrackActions.EnterOtherUsersRoom, "", 1));
    }

    [Fact]
    public void Premium_boosts_the_points_earned_after_it()
    {
        var progress = Fresh();
        RewardTrackRules.Count(Introduction, progress, RewardTrackActions.EnterOtherUsersRoom, "", 1);
        progress.Premium = true;
        RewardTrackRules.Count(Introduction, progress, RewardTrackActions.EnterOtherUsersRoom, "", 4);

        // 10 before premium, 20 × 1.2 after
        Assert.Equal(34, progress.Points);
    }

    [Fact]
    public void Another_action_counts_nothing()
    {
        var progress = Fresh();

        Assert.Empty(RewardTrackRules.Count(Introduction, progress, RewardTrackActions.Dance, "", 3));
        Assert.Equal(0, progress.Points);
    }

    [Fact]
    public void A_count_set_back_takes_no_points_and_gives_none_twice()
    {
        var progress = Fresh();
        RewardTrackRules.SetCount(Introduction, progress, VisitRooms, 5);
        RewardTrackRules.SetCount(Introduction, progress, VisitRooms, 0);
        RewardTrackRules.SetCount(Introduction, progress, VisitRooms, 5);

        Assert.Equal(60, progress.Points);
    }

    [Fact]
    public void A_premium_prize_needs_premium_as_well_as_the_points()
    {
        var progress = Fresh();
        progress.Points = 50;

        Assert.True(RewardTrackRules.IsAvailable(Introduction.Prizes[0], progress));
        Assert.False(RewardTrackRules.IsAvailable(Introduction.Prizes[1], progress));
        progress.Premium = true;
        Assert.True(RewardTrackRules.IsAvailable(Introduction.Prizes[1], progress));
    }

    [Fact]
    public void The_track_is_complete_once_every_free_prize_is_claimed()
    {
        var progress = Fresh();
        progress.Claimed.Add("free_001");

        var wire = RewardTrackRules.ToWire(Introduction, progress);

        Assert.True(wire.Complete);
        Assert.False(wire.PremiumComplete);
        Assert.True(wire.Prizes[0].Claimed);
    }
}
