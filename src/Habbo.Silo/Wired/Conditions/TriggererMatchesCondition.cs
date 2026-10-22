using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when its users are of the chosen kind (a Habbo, a pet, a bot) and among the users it compares with:
/// by default the one the box names.</summary>
[WiredBox("wf_cnd_triggerer_match")]
public class TriggererMatchesCondition : WiredUserCondition
{
    private const int CompareWith = 1;

    public override int Code => ConditionCodes.TriggererMatches;
    public override IReadOnlyList<int> DefaultIntParams => [WiredUserKinds.Habbo];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    /// <summary>The users to match, and the users to compare them with.</summary>
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users, WiredSlot.NamedUsers];

    public override bool Matches(RoomEntityStore.RoomEntity user, WiredBox box, WiredRun run) =>
        WiredUserKinds.Of(user) == box.IntParam(0, WiredUserKinds.Habbo) && run.Users(box, CompareWith).Contains(user);
}
