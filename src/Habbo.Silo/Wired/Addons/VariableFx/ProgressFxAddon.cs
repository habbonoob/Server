namespace Habbo.Silo.Wired.Addons.VariableFx;

/// <summary>A variable display of the progress category.</summary>
[WiredBox("wf_xtra_varfx_prog")]
public sealed class ProgressFxAddon : VariableFxAddon
{
    public override int Code => AddonCodes.VariableFxProgress;
    public override int Category => VariableFxStyles.Progress;
}
