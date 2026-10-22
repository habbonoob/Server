namespace Habbo.Silo.Wired.Conditions;

/// <summary>The negative of <see cref="HasHandItemCondition"/>.</summary>
[WiredBox("wf_cnd_not_has_handitem")]
public sealed class NotHasHandItemCondition : HasHandItemCondition
{
    public override int Code => ConditionCodes.NotHasHandItem;
    public override bool IsNegative => true;
}
