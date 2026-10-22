using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses the users in the room performing the chosen action right now.</summary>
[WiredBox("wf_slc_users_byaction")]
public sealed class UsersPerformingActionSelector : WiredUserSelector
{
    public override int Code => SelectorCodes.UsersPerformingAction;
    public override IReadOnlyList<int> DefaultIntParams => [WiredUserActions.Wave];

    public override IEnumerable<RoomEntityStore.RoomEntity> Select(WiredBox box, WiredRun run) =>
        run.Room.Entities.Entities.Where(user =>
            WiredUserActions.IsPerforming(user, box.IntParam(0), box.Settings.StringParam, run.Room.Entities.CurrentTick));
}
