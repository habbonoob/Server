namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires every so many periods of five seconds (1 to 120, from the dialog's slider).</summary>
[WiredBox("wf_trg_period_long")]
public sealed class RepeatLongTrigger : WiredTrigger
{
    /// <summary>Five seconds, in pulses of half a second.</summary>
    private const int PulsesPerStep = 10;

    public override int Code => TriggerCodes.PeriodicLong;
    public override IReadOnlyList<int> DefaultIntParams => [10];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e.Type == WiredEventType.Pulse
        && RepeatTrigger.CountDown(box, Math.Clamp(box.IntParam(0, 1), RepeatTrigger.MinPulses, RepeatTrigger.MaxPulses) * PulsesPerStep);
}
