using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses every user on the drawn tiles around its furni (or its users, as the dialog's
/// source chooses).</summary>
[WiredBox("wf_slc_users_neighborhood")]
public sealed class UsersInNeighborhoodSelector : WiredUserSelector
{
    public override int Code => SelectorCodes.UsersInNeighborhood;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.SelectorFurni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.SelectorUsers];
    public override IReadOnlyList<int> DefaultIntParams => WiredTiles.DefaultNeighborhood;

    public override IEnumerable<RoomEntityStore.RoomEntity> Select(WiredBox box, WiredRun run)
    {
        var tiles = WiredTiles.Around(box, WiredTiles.Origins(box, run));
        return run.Room.Entities.Entities.Where(user => tiles.Contains((user.X, user.Y)));
    }
}
