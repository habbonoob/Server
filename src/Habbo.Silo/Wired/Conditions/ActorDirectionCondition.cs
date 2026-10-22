namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its users face one of the chosen directions (a bit for each of the eight).</summary>
[WiredBox("wf_cnd_actor_dir")]
public sealed class ActorDirectionCondition : WiredUserCondition
{
    public override int Code => ConditionCodes.UserDirection;
    public override IReadOnlyList<int> DefaultIntParams => [0];

    public override bool Matches(Rooms.RoomEntityStore.RoomEntity user, WiredBox box, WiredRun run) =>
        (box.IntParam(0) & (1 << user.BodyDirection)) != 0;
}
