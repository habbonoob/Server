namespace Habbo.Silo.Wired.Conditions;

/// <summary>The negative of <see cref="UserCountInCondition"/>.</summary>
[WiredBox("wf_cnd_not_user_count")]
public sealed class UserCountNotInCondition : UserCountInCondition
{
    public override int Code => ConditionCodes.NotUserCountIn;
    public override bool IsNegative => true;
}
