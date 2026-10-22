namespace Habbo.Silo.Wired.Addons;

/// <summary>Makes its stack run one effect at a time, each in turn, so every effect has run once
/// before any runs again.</summary>
[WiredBox("wf_xtra_unseen")]
public sealed class UnseenEffectAddon : WiredAddon
{
    public override int Code => AddonCodes.Unseen;

    /// <summary>The effect whose turn it is, moving the turn on.</summary>
    public static WiredBox Next(WiredBox addon, IReadOnlyList<WiredBox> effects)
    {
        var effect = effects[addon.Rotation % effects.Count];
        addon.Rotation = (addon.Rotation + 1) % effects.Count;
        return effect;
    }
}
