using Habbo.Domain.Achievements;

namespace Habbo.Silo.Achievements;

/// <summary>Which level of an achievement to show a player, and the bar to draw for it.
/// The client is given absolute totals counted from zero and subtracts the start itself, so
/// getting the start wrong collapses the bar rather than shifting it.
///
/// Levels can be sparse. Definitions read from a capture of Habbo only carry the level the
/// captured account was on, so an achievement may say it runs to twenty levels while holding
/// the row for level three alone. Everything here works off level numbers for that reason —
/// treating the rows as a dense list would report level three's badge as level one.</summary>
public readonly record struct AchievementLevelProgress(
    int Level, int StartOfLevel, int ScoreLimit, int RewardPoints, bool IsFinal, string BadgeCode)
{
    /// <summary>Resolves the level being worked on from how many have been finished. Levels count
    /// from one; a player who has finished them all stays on the last with the bar full rather
    /// than running off the end.</summary>
    /// <param name="levelCount">How many levels the achievement has in total, which can be more
    /// than the rows on record.</param>
    public static AchievementLevelProgress Resolve(
        IReadOnlyList<AchievementLevel> levels, int levelsFinished, int levelCount)
    {
        ArgumentOutOfRangeException.ThrowIfZero(levels.Count);

        var total = Math.Max(levelCount, levels.Max(level => level.Level));
        var reached = Math.Clamp(levelsFinished, 0, total);
        var isFinal = reached >= total;
        var shown = isFinal ? total : reached + 1;

        // The nearest row is the best the hotel knows about this level. It is the row itself
        // whenever that level is on record, which is the case for every complete definition.
        var current = Nearest(levels, shown);

        // The bar runs from where this level began: the level before it, or zero for the first.
        // A level before it that is not on record leaves the bar starting at zero, which reads
        // as a longer climb rather than as a broken one.
        var previous = shown <= 1 ? null : levels.FirstOrDefault(level => level.Level == shown - 1);

        return new AchievementLevelProgress(
            shown,
            previous?.RequiredProgress ?? 0,
            current.RequiredProgress,
            current.RewardPoints,
            isFinal,
            current.Level == shown ? current.BadgeCode : string.Empty);
    }

    private static AchievementLevel Nearest(IReadOnlyList<AchievementLevel> levels, int level) =>
        levels.MinBy(candidate => Math.Abs(candidate.Level - level))!;
}
