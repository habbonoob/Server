namespace Habbo.Silo.Wired.Effects;

/// <summary>Moves each of its furni one tile away from the nearest user in its row or column, as Chase picks them
/// (Habbo: aligned, it stepped away; with no one in line it moved too, once measured).</summary>
[WiredBox("wf_act_flee")]
public sealed class FleeEffect : ChaseEffect
{
    public override int Code => EffectCodes.Flee;
    protected override int Towards => -1;
}
