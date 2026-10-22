namespace Habbo.Silo.Wired.Effects;

/// <summary>Writes to the room's wired log when the stack's conditions fail.</summary>
[WiredBox("wf_act_neg_log")]
public sealed class NegativeWriteLogEffect : WriteLogEffect
{
    public override int Code => EffectCodes.NegativeWriteToLogs;
    public override bool IsNegative => true;
}
