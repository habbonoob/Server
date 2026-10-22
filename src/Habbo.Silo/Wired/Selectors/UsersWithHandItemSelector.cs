using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses every user holding the chosen hand item.</summary>
[WiredBox("wf_slc_users_handitem")]
public sealed class UsersWithHandItemSelector : WiredUserSelector
{
    public override int Code => SelectorCodes.UsersWithHandItem;
    public override IReadOnlyList<int> DefaultIntParams => [2];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override IEnumerable<RoomEntityStore.RoomEntity> Select(WiredBox box, WiredRun run) =>
        run.Room.Entities.Entities.Where(user => user.HandItem == box.IntParam(0));
}
