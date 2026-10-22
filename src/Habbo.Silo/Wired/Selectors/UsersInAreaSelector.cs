using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses every user standing in the drawn area.</summary>
[WiredBox("wf_slc_users_area")]
public sealed class UsersInAreaSelector : WiredUserSelector
{
    public override int Code => SelectorCodes.UsersInArea;
    public override IReadOnlyList<int> DefaultIntParams => [0, 0, 0, 0];

    public override IEnumerable<RoomEntityStore.RoomEntity> Select(WiredBox box, WiredRun run) =>
        run.Room.Entities.Entities.Where(user => WiredTiles.InArea(box, user.X, user.Y));
}
