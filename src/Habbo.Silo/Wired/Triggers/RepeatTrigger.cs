namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires every so many pulses of half a second (1 to 120, from the dialog's slider).</summary>
[WiredBox("wf_trg_periodically")]
public sealed class RepeatTrigger : WiredTrigger
{
    public const int MinPulses = 1;
    public const int MaxPulses = 120;

    public override int Code => TriggerCodes.TriggerPeriodically;
    public override IReadOnlyList<int> DefaultIntParams => [MinPulses];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e.Type == WiredEventType.Pulse && CountDown(box, Math.Clamp(box.IntParam(0, MinPulses), MinPulses, MaxPulses));

    /// <summary>Steps a periodic box's countdown: true, and restarted, when it runs out.</summary>
    public static bool CountDown(WiredBox box, int period)
    {
        if (--box.PulsesUntilNext > 0)
            return false;

        box.PulsesUntilNext = period;
        return true;
    }
}
