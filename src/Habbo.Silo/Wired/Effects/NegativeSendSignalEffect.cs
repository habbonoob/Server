namespace Habbo.Silo.Wired.Effects;

/// <summary>Sends its signal when its stack's conditions fail.</summary>
[WiredBox("wf_act_neg_send_signal")]
public sealed class NegativeSendSignalEffect : SendSignalEffect
{
    public override int Code => EffectCodes.NegativeSendSignal;
    public override bool IsNegative => true;
}
