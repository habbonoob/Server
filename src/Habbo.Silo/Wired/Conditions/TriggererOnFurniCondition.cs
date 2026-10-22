namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its users (the one the stack ran for, unless selectors chose others) stand
/// on one of its furni.</summary>
[WiredBox("wf_cnd_trggrer_on_frn")]
public class TriggererOnFurniCondition : WiredUserCondition
{
    public override int Code => ConditionCodes.TriggererIsOnFurni;
    public override bool PicksFurni => true;

    public override bool Matches(Rooms.RoomEntityStore.RoomEntity user, WiredBox box, WiredRun run) =>
        run.Furni(box).Any(item => WiredFurni.IsUnder(user, item));
}
