using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses every furni in the room of the same kind as one of the picked furni; with its
/// checkbox ticked, only those also showing the same state.</summary>
[WiredBox("wf_slc_furni_bytype")]
public sealed class FurniByTypeSelector : WiredFurniSelector
{
    private const int MatchState = 0;

    public override int Code => SelectorCodes.FurniByType;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.SelectorFurni];
    public override IReadOnlyList<int> DefaultIntParams => [0];

    public override IEnumerable<RoomItemStore.PlacedItem> Select(WiredBox box, WiredRun run)
    {
        var matchState = box.Flag(MatchState);
        var kinds = run.Furni(box)
            .Select(item => (item.Item.DefinitionId, State: matchState ? item.Data.StateText : null))
            .ToHashSet();
        return run.Room.Items.Items.Where(item =>
            kinds.Contains((item.Item.DefinitionId, matchState ? item.Data.StateText : null)));
    }
}
