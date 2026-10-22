using Habbo.Silo.Rooms;
using Habbo.Silo.Wired.Conditions;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses every furni standing lower than, at, or higher than the chosen altitude
/// (in hundredths, 0 to 80).</summary>
[WiredBox("wf_slc_furni_altitude")]
public sealed class FurniAltitudeSelector : WiredFurniSelector
{
    private const double Hundredths = 100.0;

    public override int Code => SelectorCodes.FurniWithAltitude;
    public override IReadOnlyList<int> DefaultIntParams => [0, WiredComparison.LowerThan];

    public override IEnumerable<RoomItemStore.PlacedItem> Select(WiredBox box, WiredRun run) =>
        run.Room.Items.Items.Where(item =>
            item.Item.Definition!.Type == Domain.Furniture.ItemType.Floor
            && WiredComparison.Holds(box.IntParam(1), (int)Math.Round(item.Z * Hundredths), box.IntParam(0)));
}
