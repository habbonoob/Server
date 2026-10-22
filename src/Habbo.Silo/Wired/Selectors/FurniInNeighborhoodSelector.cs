using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses every furni on the drawn tiles around its furni (or its users, as the dialog's
/// source chooses).</summary>
[WiredBox("wf_slc_furni_neighborhood")]
public sealed class FurniInNeighborhoodSelector : WiredFurniSelector
{
    public override int Code => SelectorCodes.FurniInNeighborhood;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.SelectorFurni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.SelectorUsers];
    public override IReadOnlyList<int> DefaultIntParams => WiredTiles.DefaultNeighborhood;

    public override IEnumerable<RoomItemStore.PlacedItem> Select(WiredBox box, WiredRun run)
    {
        var tiles = WiredTiles.Around(box, WiredTiles.Origins(box, run));
        return run.Room.Items.Items.Where(item => RoomItemStore.Footprint(item).Any(tiles.Contains));
    }
}
