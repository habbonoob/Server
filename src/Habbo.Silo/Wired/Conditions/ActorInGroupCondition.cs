namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its users are members of the chosen group, or with none chosen the room's.</summary>
[WiredBox("wf_cnd_actor_in_group")]
public class ActorInGroupCondition : WiredUserCondition
{
    public override int Code => ConditionCodes.ActorIsGroupMember;

    public override bool Matches(Rooms.RoomEntityStore.RoomEntity user, WiredBox box, WiredRun run) =>
        WiredGroups.Chosen(box, run.Room) is { } group && user.GroupIds.Contains(group);
}
