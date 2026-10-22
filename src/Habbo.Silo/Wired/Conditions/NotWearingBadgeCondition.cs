namespace Habbo.Silo.Wired.Conditions;

/// <summary>The negative of <see cref="WearingBadgeCondition"/>.</summary>
[WiredBox("wf_cnd_not_wearing_b")]
public sealed class NotWearingBadgeCondition : WearingBadgeCondition
{
    public override int Code => ConditionCodes.NotActorIsWearingBadge;
    public override bool IsNegative => true;
}
