using Habbo.Domain.RewardTracks;
using Habbo.Protocol.Messages.Outgoing.RewardTrack;

namespace Habbo.Silo.RewardTracks;

/// <summary>How a player's progress on a track moves: a task counts up to its last level, each level its count
/// passes gives its points (more, by the premium boost, to one who owns premium), and the points unlock prizes.</summary>
public static class RewardTrackRules
{
    /// <summary>A task whose count changed, and the points it gave.</summary>
    public sealed record TaskCounted(RewardTrackTask Task, int Count, int PointsGained);

    /// <summary>Counts an action on every task of the track that counts it.</summary>
    public static List<TaskCounted> Count(RewardTrack track, RewardTrackProgress progress, string action, string parameter, int amount)
    {
        var counted = new List<TaskCounted>();
        if (amount <= 0)
            return counted;

        foreach (var task in track.Tasks.Where(t => t.Action == action && (t.Parameter.Length == 0 || t.Parameter == parameter)))
        {
            if (SetCount(track, progress, task, CountOf(progress, task) + amount) is { } changed)
                counted.Add(changed);
        }

        return counted;
    }

    /// <summary>Sets a task's count, as wired's "progress reward track" does: to the score, or by it when it adds to the
    /// count. Null when the count did not change.</summary>
    public static TaskCounted? SetCount(RewardTrack track, RewardTrackProgress progress, RewardTrackTask task, int count)
    {
        if (task.Premium && !progress.Premium)
            return null;

        var before = CountOf(progress, task);
        var after = Math.Clamp(count, 0, task.MaxCount);
        if (after == before)
            return null;

        // only a level passed for the first time gives points; a count set back passes none
        var gained = task.Levels
            .Where(l => l.Count > before && l.Count <= after && (!l.Premium || progress.Premium))
            .Sum(l => Boosted(track, progress, l.Points));
        progress.Tasks[task.Id] = after;
        progress.Points += gained;
        return new TaskCounted(task, after, gained);
    }

    public static int CountOf(RewardTrackProgress progress, RewardTrackTask task) => progress.Tasks.GetValueOrDefault(task.Id);

    /// <summary>A level's points: multiplied by the boost for one who owns premium (Habbo's own: 1166 points on the
    /// introduction track, between its 1050 unboosted and 1260 boosted, so premium bought along the way boosts only what
    /// came after).</summary>
    private static int Boosted(RewardTrack track, RewardTrackProgress progress, int points) =>
        progress.Premium && track.Premium is { } premium
            ? (int)Math.Round(points * premium.TaskPointsBoost, MidpointRounding.AwayFromZero)
            : points;

    /// <summary>Whether the player may claim the prize: enough points, and premium for a premium one.</summary>
    public static bool IsAvailable(RewardTrackPrize prize, RewardTrackProgress progress) =>
        progress.Points >= prize.Points && (!prize.Premium || progress.Premium);

    /// <summary>The track as the player sees it.</summary>
    public static RewardTrackData ToWire(RewardTrack track, RewardTrackProgress progress) => new(
        track.Id,
        track.Theme,
        progress.Points,
        track.Premium is { } premium
            ? new RewardTrackPremiumData(premium.TaskPointsBoost, premium.InstantPoints, premium.CostDiamonds, premium.CostCredits)
            : null,
        progress.Premium,
        Complete: track.Prizes.Where(p => !p.Premium).All(p => progress.Claimed.Contains(p.Id)),
        PremiumComplete: track.Prizes.Where(p => p.Premium).All(p => progress.Claimed.Contains(p.Id)),
        track.Tasks.Select(task => new RewardTrackTaskData(task.Id, task.Action, task.Parameter, CountOf(progress, task), task.Premium,
            task.Levels.Select(l => new RewardTrackTaskLevelData(l.Count, l.Points, l.Premium)).ToList())).ToList(),
        track.Prizes.Select(prize => new RewardTrackPrizeData(prize.Id, prize.Points, prize.Type, prize.Item, prize.Extra, prize.Amount,
            prize.Premium, IsAvailable(prize, progress), progress.Claimed.Contains(prize.Id))).ToList());
}
