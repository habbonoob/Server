namespace Habbo.Silo.Wired.Conditions;

/// <summary>The negative of <see cref="StuffIsCondition"/>: the furni the stack ran for is of none
/// of the picked furni's kinds.</summary>
[WiredBox("wf_cnd_not_stuff_is")]
public sealed class StuffIsNotCondition : StuffIsCondition
{
    public override int Code => ConditionCodes.NotStuffTypeMatches;
    public override bool IsNegative => true;
}
