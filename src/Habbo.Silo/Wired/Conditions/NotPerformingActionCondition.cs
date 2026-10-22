namespace Habbo.Silo.Wired.Conditions;

/// <summary>The negative of <see cref="PerformingActionCondition"/>.</summary>
[WiredBox("wf_cnd_not_user_performs_action")]
public sealed class NotPerformingActionCondition : PerformingActionCondition
{
    public override int Code => ConditionCodes.NotPerformingAction;
    public override bool IsNegative => true;
}
