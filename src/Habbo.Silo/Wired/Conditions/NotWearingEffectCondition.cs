namespace Habbo.Silo.Wired.Conditions;

/// <summary>The negative of <see cref="WearingEffectCondition"/>.</summary>
[WiredBox("wf_cnd_not_wearing_fx")]
public sealed class NotWearingEffectCondition : WearingEffectCondition
{
    public override int Code => ConditionCodes.NotActorIsWearingEffect;
    public override bool IsNegative => true;
}
