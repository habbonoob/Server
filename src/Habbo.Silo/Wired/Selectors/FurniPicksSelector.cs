using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses the furni picked in its dialog.</summary>
[WiredBox("wf_slc_furni_picks")]
public sealed class FurniPicksSelector : WiredFurniSelector
{
    public override int Code => SelectorCodes.FurniPicks;
    public override bool AllowsWallFurni => true;
    public override bool PicksFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots => [];
    public override bool TakesPicks => true;

    public override IEnumerable<RoomItemStore.PlacedItem> Select(WiredBox box, WiredRun run) => run.Room.Picked(box);
}
