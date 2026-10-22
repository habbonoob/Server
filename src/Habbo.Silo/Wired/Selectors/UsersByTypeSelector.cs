using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses every user of the chosen kind: Habbos, pets or bots.</summary>
[WiredBox("wf_slc_users_bytype")]
public sealed class UsersByTypeSelector : WiredUserSelector
{
    public override int Code => SelectorCodes.UsersByType;
    public override IReadOnlyList<int> DefaultIntParams => [WiredUserKinds.Habbo];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override IEnumerable<RoomEntityStore.RoomEntity> Select(WiredBox box, WiredRun run) =>
        run.Room.Entities.Entities.Where(user => WiredUserKinds.Of(user) == box.IntParam(0, WiredUserKinds.Habbo));
}
