namespace Habbo.Silo.Wired.Addons.VariableFx;

/// <summary>A variable display of the status category.</summary>
[WiredBox("wf_xtra_varfx_status")]
public sealed class StatusFxAddon : VariableFxAddon
{
    public override int Code => AddonCodes.VariableFxStatus;
    public override int Category => VariableFxStyles.Status;
}
