namespace Habbo.Silo.Wired.Addons.VariableFx;

/// <summary>A variable display that shows the value as a number, with an icon (the box's text) to the left, the right or
/// both sides.</summary>
[WiredBox("wf_xtra_varfx_number")]
public sealed class NumberFxAddon : VariableFxAddon
{
    private static readonly string[] Alignments = ["left", "right", "double"];

    public override int Code => AddonCodes.VariableFxNumber;
    public override int Category => VariableFxStyles.Number;
    public override IReadOnlyList<int> DefaultIntParams => [.. base.DefaultIntParams, 0];

    protected override IEnumerable<(string Key, string Value)> OwnExtras(WiredBox box, VariableFxStyle style, int renderer)
    {
        foreach (var extra in base.OwnExtras(box, style, renderer))
            yield return extra;
        if (box.Settings.StringParam.Trim() is { Length: > 0 } icon)
            yield return ("icon", icon);
        if (box.IntParam(Extra) is > 0 and < 3 and var alignment)
            yield return ("icon_alignment", Alignments[alignment]);
    }

    // a number shows no range
    protected override (long Min, long Max) Range(WiredBox box) => (Long(box, 10), Long(box, 12));
}
