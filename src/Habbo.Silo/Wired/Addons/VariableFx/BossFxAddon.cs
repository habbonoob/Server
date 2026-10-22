namespace Habbo.Silo.Wired.Addons.VariableFx;

/// <summary>A variable display of the boss category.</summary>
[WiredBox("wf_xtra_varfx_boss")]
public sealed class BossFxAddon : VariableFxAddon
{
    public override int Code => AddonCodes.VariableFxBoss;
    public override int Category => VariableFxStyles.Boss;
}
