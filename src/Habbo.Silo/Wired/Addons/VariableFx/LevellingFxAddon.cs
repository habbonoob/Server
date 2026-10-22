using System.Globalization;
using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.VariableFx;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons.VariableFx;

/// <summary>A variable display that shows a level and the way to the next: the variable's level system (a level-up add-on
/// in the variable's stack) gives each holder its level, the range of experience it is in, and whether it is maxed. Its
/// config sends no range (-1, -1), as Habbo's does; its sub-renderer is its 22nd setting, as its style allows.</summary>
[WiredBox("wf_xtra_varfx_levelling")]
public sealed class LevellingFxAddon : VariableFxAddon
{
    private const int NoRange = -1;

    public override int Code => AddonCodes.VariableFxLevelling;
    public override int Category => VariableFxStyles.Levelling;
    public override IReadOnlyList<int> DefaultIntParams => [.. base.DefaultIntParams, 2];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    protected override IEnumerable<(string Key, string Value)> OwnExtras(WiredBox box, VariableFxStyle style, int renderer)
    {
        foreach (var extra in base.OwnExtras(box, style, renderer))
            yield return extra;

        // the first style draws its bar with 2, 3 or 4 (2 unless chosen); the second only with 1
        var chosen = box.IntParam(Extra);
        var sub = style.StyleId == 0 ? chosen is 2 or 3 or 4 ? chosen : 2 : 1;
        yield return ("sub_renderer", sub.ToString(CultureInfo.InvariantCulture));
    }

    protected override (long Min, long Max) Range(WiredBox box) => (NoRange, NoRange);

    protected override (VariableFxRangeData? Range, IReadOnlyList<(string Key, string Value)> Extras) Overrides(
        WiredBox box, WiredVariable variable, WiredHolder holder, int value, IWiredRoom room)
    {
        var levels = room.StackAt(box.Item.X, box.Item.Y)?.FirstOrDefault(other => other.Type is VariableLevelUpAddon);
        if (levels is null || VariableLevelUpAddon.Level(levels, value) is not { } level)
            return (null, []);

        return (new VariableFxRangeData(level.LevelXp, level.NextXp),
        [
            ("max_level", level.MaxLevel.ToString(CultureInfo.InvariantCulture)),
            ("current_level", level.Current.ToString(CultureInfo.InvariantCulture)),
            ("is_maxed", level.Maxed ? "true" : "false"),
        ]);
    }
}
