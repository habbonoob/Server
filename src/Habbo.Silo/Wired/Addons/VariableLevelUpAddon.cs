using System.Globalization;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons;

/// <summary>In a variable box's stack, reads the variable's value as experience and gives it the
/// sub-variables of a level system: the level, the progress through it, what the next one needs.
/// The levels follow a straight line, a growing curve, or the dialog's own table (the client's
/// Linear-, Exponential- and InterpolateLevelUpper, whose sums these are).</summary>
[WiredBox("wf_xtra_var_lvlup_system")]
public sealed class VariableLevelUpAddon : WiredAddon
{
    private const int SubVariableMask = 0;
    private const int Mode = 1;

    private const int Manual = 0;
    private const int Linear = 1;
    private const int Exponential = 2;

    private const int PercentScale = 100;

    /// <summary>The most levels a system may have, and the least a curve may grow by: a level is found by halving
    /// the range, but a system of two billion levels, or a curve that never rises, is no level system at all.</summary>
    private const int MostLevels = 1000;
    private const double LeastStrength = 0.01;

    public override int Code => AddonCodes.VariableLevelUp;
    public override IReadOnlyList<int> DefaultIntParams => [0, Linear, 100, 50, 50];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    /// <summary>The sub-variables the dialog offers, by their bit.</summary>
    private static readonly (int Bit, string Name)[] Offered =
    [
        (0, "current_level"), (1, "current_xp"), (2, "progress"), (3, "progress_percentage"),
        (4, "xp_required"), (5, "xp_remaining"), (6, "is_maxed"), (7, "max_level"),
    ];

    public static IEnumerable<WiredVariable> SubVariables(WiredBox addon, WiredVariable parent)
    {
        if (!parent.HasValue || Curve(addon) is not { } curve)
            yield break;

        foreach (var (bit, name) in Offered.Where(o => (addon.IntParam(SubVariableMask) & (1 << o.Bit)) != 0))
            yield return WiredSubVariables.Of(parent, name, (value, _) => value is { } xp ? Read(curve, name, xp) : null);
    }

    /// <summary>Where the experience stands in the add-on's level system: its level, the experience that level and the
    /// next start at (the same when maxed), the last level; null for a table that does not read.</summary>
    public static (int Current, long LevelXp, long NextXp, int MaxLevel, bool Maxed)? Level(WiredBox addon, int xp) =>
        Curve(addon) is { } curve ? Level(curve, xp) : null;

    private static (int Current, long LevelXp, long NextXp, int MaxLevel, bool Maxed) Level((Func<int, double> XpFor, int MaxLevel) curve, int xp)
    {
        // the highest level whose experience is reached; the curve rises, so the range is halved to find it
        var (level, above) = (1, curve.MaxLevel + 1);
        while (above - level > 1)
        {
            var middle = level + (above - level) / 2;
            if (curve.XpFor(middle) <= xp)
                level = middle;
            else
                above = middle;
        }

        var maxed = level >= curve.MaxLevel;
        var levelXp = (long)curve.XpFor(level);
        return (level, levelXp, maxed ? levelXp : (long)curve.XpFor(level + 1), curve.MaxLevel, maxed);
    }

    private static int Read((Func<int, double> XpFor, int MaxLevel) curve, string name, int xp)
    {
        var (level, levelXp, nextXp, _, maxed) = Level(curve, xp);
        var required = nextXp - levelXp;
        var progress = xp - levelXp;
        long result = name switch
        {
            "current_level" => level,
            "current_xp" => xp,
            "progress" => maxed ? 0 : progress,
            "progress_percentage" => maxed ? PercentScale : required == 0 ? 0 : progress * PercentScale / required,
            "xp_required" => required,
            "xp_remaining" => maxed ? 0 : nextXp - xp,
            "is_maxed" => maxed ? 1 : 0,
            _ => curve.MaxLevel,
        };
        return (int)Math.Clamp(result, int.MinValue, int.MaxValue);
    }

    /// <summary>The experience each level starts at, and the last level; null for a table that does not read.</summary>
    private static (Func<int, double> XpFor, int MaxLevel)? Curve(WiredBox addon)
    {
        switch (addon.IntParam(Mode))
        {
            case Linear:
            {
                var step = Math.Max(1, addon.IntParam(2, 100));
                return (level => level <= 1 ? 0 : (double)step * (level - 1), Math.Clamp(addon.IntParam(3, 50), 2, MostLevels));
            }
            case Exponential:
            {
                var first = Math.Max(1, addon.IntParam(2, 100));
                var strength = Math.Max(LeastStrength, addon.IntParam(3, 20) / 100.0);
                return (level => level < 1 ? 0 : first * ((Math.Pow(1 + strength, level - 1) - 1 + 1e-9) / strength),
                    Math.Clamp(addon.IntParam(4, 50), 2, MostLevels));
            }
            case Manual:
                return Table(addon.Settings.StringParam);
            default:
                return null;
        }
    }

    /// <summary>The dialog's "level=xp" lines, levels and experience both rising, level 1 at none.</summary>
    private static (Func<int, double> XpFor, int MaxLevel)? Table(string text)
    {
        var points = new SortedDictionary<int, int> { [1] = 0 };
        var (lastLevel, lastXp) = (1, 0);
        foreach (var line in text.Split('\n'))
        {
            var parts = line.Trim().Split('=', 2);
            if (parts.Length < 2
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var level)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var xp))
                continue;
            if (level <= lastLevel || xp <= lastXp || level > MostLevels)
                return null;
            points[level] = xp;
            (lastLevel, lastXp) = (level, xp);
        }

        if (points.Count < 2)
            return null;

        // between the table's levels, experience runs in a straight line
        double XpFor(int level)
        {
            if (level <= 1)
                return 0;
            var below = points.Last(p => p.Key <= level);
            if (below.Key == level)
                return below.Value;
            var above = points.FirstOrDefault(p => p.Key > level);
            if (above.Key == 0)
                return below.Value;
            return below.Value + (int)((above.Value - below.Value) / (double)(above.Key - below.Key) * (level - below.Key));
        }

        return (XpFor, lastLevel);
    }
}
