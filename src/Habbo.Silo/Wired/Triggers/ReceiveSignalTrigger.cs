namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when a signal arrives on one of its antennas.</summary>
[WiredBox("wf_trg_recv_signal")]
public sealed class ReceiveSignalTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.ReceiveSignal;
    public override IReadOnlyList<WiredSlot> FurniSlots =>
        [new([WiredSources.PickedFurni, WiredSources.SelectedFurni, WiredSources.SignalFurni], WiredSources.PickedFurni)];
    public override bool PicksFurni => true;
    public override WiredPickRule? PickRule => new(WiredFurni.IsAntenna, "wiredfurni.error.require_antenna_furni");

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.SignalReceived, Item: { } antenna } && Watches(box, antenna, e, room);
}
