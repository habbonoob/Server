namespace Habbo.Silo.Wired.Conditions;

/// <summary>The negative of <see cref="HasVariableCondition"/>.</summary>
[WiredBox("wf_cnd_neg_has_var")]
public sealed class HasNoVariableCondition : HasVariableCondition
{
    public override int Code => ConditionCodes.NotHasVariable;
    public override bool IsNegative => true;
}
