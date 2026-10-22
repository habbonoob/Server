namespace Habbo.Silo.Wired.Addons.VariableFx;

/// <summary>A variable display of the health category.</summary>
[WiredBox("wf_xtra_varfx_hp")]
public sealed class HealthFxAddon : VariableFxAddon
{
    public override int Code => AddonCodes.VariableFxHealth;
    public override int Category => VariableFxStyles.Health;
}
