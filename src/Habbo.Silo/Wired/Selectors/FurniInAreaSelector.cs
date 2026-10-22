using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses every furni standing in the drawn area.</summary>
[WiredBox("wf_slc_furni_area")]
public sealed class FurniInAreaSelector : WiredFurniSelector
{
    public override int Code => SelectorCodes.FurniInArea;
    public override IReadOnlyList<int> DefaultIntParams => [0, 0, 0, 0];

    public override IEnumerable<RoomItemStore.PlacedItem> Select(WiredBox box, WiredRun run) =>
        run.Room.Items.Items.Where(item => RoomItemStore.Footprint(item).Any(tile => WiredTiles.InArea(box, tile.X, tile.Y)));
}
