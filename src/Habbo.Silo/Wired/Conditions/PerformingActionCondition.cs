namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its users are performing the chosen action right now.</summary>
[WiredBox("wf_cnd_user_performs_action")]
public class PerformingActionCondition : WiredUserCondition
{
    public override int Code => ConditionCodes.PerformingAction;
    public override IReadOnlyList<int> DefaultIntParams => [WiredUserActions.Wave];

    public override bool Matches(Rooms.RoomEntityStore.RoomEntity user, WiredBox box, WiredRun run) =>
        WiredUserActions.IsPerforming(user, box.IntParam(0), box.Settings.StringParam, run.Room.Entities.CurrentTick);
}
