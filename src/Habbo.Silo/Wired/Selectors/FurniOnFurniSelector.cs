using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses the furni on the tiles of its furni: those above them, under them, at the same
/// height, or all of them, as the dialog chooses.</summary>
[WiredBox("wf_slc_furni_onfurni")]
public sealed class FurniOnFurniSelector : WiredFurniSelector
{
    // onfurni.0 to .3
    private const int Above = 0;
    private const int Under = 1;
    private const int SameHeight = 2;

    private const double Tolerance = 0.01;

    public override int Code => SelectorCodes.FurniOnFurni;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.SelectorFurni];
    public override IReadOnlyList<int> DefaultIntParams => [Above];

    public override IEnumerable<RoomItemStore.PlacedItem> Select(WiredBox box, WiredRun run)
    {
        var mode = box.IntParam(0);
        var chosen = new HashSet<RoomItemStore.PlacedItem>();
        foreach (var item in run.Furni(box))
        {
            var others = RoomItemStore.Footprint(item)
                .SelectMany(tile => run.Room.Items.FloorItemsOn(tile.X, tile.Y))
                .Where(other => other != item);
            chosen.UnionWith(others.Where(other => mode switch
            {
                Above => other.Z >= RoomItemStore.TopOf(item) - Tolerance,
                Under => RoomItemStore.TopOf(other) <= item.Z + Tolerance,
                SameHeight => Math.Abs(other.Z - item.Z) < Tolerance,
                _ => true,
            }));
        }

        return chosen;
    }
}
