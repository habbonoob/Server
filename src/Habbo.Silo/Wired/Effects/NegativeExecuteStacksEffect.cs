namespace Habbo.Silo.Wired.Effects;

/// <summary>Runs the picked stacks' effects when its own stack's conditions fail.</summary>
[WiredBox("wf_act_neg_call_stacks")]
public sealed class NegativeExecuteStacksEffect : ExecuteStacksEffect
{
    public override int Code => EffectCodes.NegativeCallAnotherStack;
    public override bool IsNegative => true;
}
