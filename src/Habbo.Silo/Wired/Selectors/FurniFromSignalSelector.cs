using Habbo.Silo.Rooms;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>Chooses the furni the signal that started its stack carried.</summary>
[WiredBox("wf_slc_furni_signal")]
public sealed class FurniFromSignalSelector : WiredFurniSelector
{
    public override int Code => SelectorCodes.FurniFromSignal;

    public override IEnumerable<RoomItemStore.PlacedItem> Select(WiredBox box, WiredRun run) =>
        run.Event.SignalFurni.Where(item => run.Room.FindItem(item.Item.Id) == item);
}
