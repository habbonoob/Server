namespace Habbo.Silo.Wired.Addons;

/// <summary>Makes its stack run only some of its effects, chosen at random: after skipping the
/// first so many, it picks so many (the dialog's two sliders, 0 to 100 and 1 to 100).</summary>
[WiredBox("wf_xtra_random")]
public sealed class RandomEffectAddon : WiredAddon
{
    private const int Skips = 0;
    private const int Picks = 1;

    public override int Code => AddonCodes.PickOneRandomly;
    public override IReadOnlyList<int> DefaultIntParams => [0, 1];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    /// <summary>The effects the stack runs this time: so many, at random, avoiding those picked in
    /// the last so many runs while others are left.</summary>
    public static List<WiredBox> Choose(WiredBox addon, IReadOnlyList<WiredBox> effects, Random random)
    {
        var avoid = Math.Clamp(addon.IntParam(Skips), 0, 100);
        var recent = addon.RecentPicks.TakeLast(avoid).SelectMany(ids => ids).ToHashSet();
        var candidates = effects.Where(effect => !recent.Contains(effect.Id)).ToList();
        if (candidates.Count == 0)
            candidates = effects.ToList();

        var picked = candidates.OrderBy(_ => random.Next()).Take(Math.Max(1, addon.IntParam(Picks, 1))).ToList();
        addon.RecentPicks.Add(picked.Select(effect => effect.Id).ToList());
        if (addon.RecentPicks.Count > avoid)
            addon.RecentPicks.RemoveRange(0, addon.RecentPicks.Count - avoid);
        return picked;
    }
}
