namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its users wear the chosen avatar effect.</summary>
[WiredBox("wf_cnd_wearing_effect")]
public class WearingEffectCondition : WiredUserCondition
{
    public override int Code => ConditionCodes.ActorIsWearingEffect;
    public override IReadOnlyList<int> DefaultIntParams => [0];

    public override bool Matches(Rooms.RoomEntityStore.RoomEntity user, WiredBox box, WiredRun run) =>
        user.EffectId == box.IntParam(0);
}
