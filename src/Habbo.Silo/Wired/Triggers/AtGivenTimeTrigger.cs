namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires once, when the room's wired timer reaches the box's time (1 to 1200 pulses of half
/// a second). "Reset timers" starts the count again. The long version opens the same dialog, so
/// it is the same box.</summary>
[WiredBox("wf_trg_at_given_time", "wf_trg_at_time_long")]
public sealed class AtGivenTimeTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.TriggerOnce;
    public override IReadOnlyList<int> DefaultIntParams => [1];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e.Type == WiredEventType.Pulse && room.TimerPulses == box.IntParam(0, 1);
}
