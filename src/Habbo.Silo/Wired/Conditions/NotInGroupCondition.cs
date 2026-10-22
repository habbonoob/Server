namespace Habbo.Silo.Wired.Conditions;

/// <summary>The negative of <see cref="ActorInGroupCondition"/>.</summary>
[WiredBox("wf_cnd_not_in_group")]
public sealed class NotInGroupCondition : ActorInGroupCondition
{
    public override int Code => ConditionCodes.NotActorIsGroupMember;
    public override bool IsNegative => true;
}
