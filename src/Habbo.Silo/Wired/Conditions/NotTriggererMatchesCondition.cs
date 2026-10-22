namespace Habbo.Silo.Wired.Conditions;

/// <summary>The negative of <see cref="TriggererMatchesCondition"/>.</summary>
[WiredBox("wf_cnd_not_triggerer_match")]
public sealed class NotTriggererMatchesCondition : TriggererMatchesCondition
{
    public override int Code => ConditionCodes.NotTriggererMatches;
    public override bool IsNegative => true;
}
