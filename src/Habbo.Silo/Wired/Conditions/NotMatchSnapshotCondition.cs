namespace Habbo.Silo.Wired.Conditions;

/// <summary>The negative of <see cref="MatchSnapshotCondition"/>.</summary>
[WiredBox("wf_cnd_not_match_snap")]
public sealed class NotMatchSnapshotCondition : MatchSnapshotCondition
{
    public override int Code => ConditionCodes.NotStatesMatch;
    public override bool IsNegative => true;
}
