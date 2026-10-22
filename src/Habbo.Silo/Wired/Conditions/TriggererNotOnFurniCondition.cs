namespace Habbo.Silo.Wired.Conditions;

/// <summary>The negative of <see cref="TriggererOnFurniCondition"/>.</summary>
[WiredBox("wf_cnd_not_trggrer_on")]
public sealed class TriggererNotOnFurniCondition : TriggererOnFurniCondition
{
    public override int Code => ConditionCodes.NotTriggererIsOnFurni;
    public override bool IsNegative => true;
}
