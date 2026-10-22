namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its users hold the chosen hand item.</summary>
[WiredBox("wf_cnd_has_handitem")]
public class HasHandItemCondition : WiredUserCondition
{
    public override int Code => ConditionCodes.ActorHasHandItem;
    public override IReadOnlyList<int> DefaultIntParams => [2];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Matches(Rooms.RoomEntityStore.RoomEntity user, WiredBox box, WiredRun run) =>
        user.HandItem == box.IntParam(0);
}
