namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when the number of users in the room is within the box's range (inclusive);
/// the negative box passes when it is outside it.</summary>
[WiredBox("wf_cnd_user_count_in")]
public class UserCountInCondition : WiredCondition
{
    public override int Code => ConditionCodes.UserCountIn;
    public override IReadOnlyList<int> DefaultIntParams => [1, 50];
    public override IReadOnlyList<int> ResetIntParams => [50];

    public override bool Passes(WiredBox box, WiredRun run) =>
        (run.Room.Entities.Count >= box.IntParam(0, 1) && run.Room.Entities.Count <= box.IntParam(1, int.MaxValue)) != IsNegative;
}
