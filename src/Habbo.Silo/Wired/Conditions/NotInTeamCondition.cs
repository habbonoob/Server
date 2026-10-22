namespace Habbo.Silo.Wired.Conditions;

/// <summary>The negative of <see cref="ActorInTeamCondition"/>.</summary>
[WiredBox("wf_cnd_not_in_team")]
public sealed class NotInTeamCondition : ActorInTeamCondition
{
    public override int Code => ConditionCodes.NotActorIsInTeam;
    public override bool IsNegative => true;
}
