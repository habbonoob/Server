namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its users wear the badge the box names.</summary>
[WiredBox("wf_cnd_wearing_badge")]
public class WearingBadgeCondition : WiredUserCondition
{
    public override int Code => ConditionCodes.ActorIsWearingBadge;

    public override bool Matches(Rooms.RoomEntityStore.RoomEntity user, WiredBox box, WiredRun run) =>
        box.Settings.StringParam.Trim() is { Length: > 0 } badge && user.WornBadges.Contains(badge, StringComparer.OrdinalIgnoreCase);
}
