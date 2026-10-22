namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires when one of its counters reaches the box's time (seconds, minutes and the half
/// second between, as the dialog stores them).</summary>
[WiredBox("wf_trg_clock_counter")]
public sealed class ClockReachesTimeTrigger : WiredTrigger
{
    public override int Code => TriggerCodes.ClockReachTime;
    public override bool PicksFurni => true;
    public override WiredPickRule? PickRule => WiredGames.Clocks;
    public override IReadOnlyList<int> DefaultIntParams => [0, 0, 0];

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e is { Type: WiredEventType.ClockTicked, Item: { } clock }
        && Watches(box, clock, e, room)
        && room.Games.Clock(clock)?.Pulses == WiredGames.Pulses(box.IntParam(0), box.IntParam(1), box.IntParam(2));
}
