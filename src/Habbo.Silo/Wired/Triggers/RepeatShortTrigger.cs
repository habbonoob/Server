namespace Habbo.Silo.Wired.Triggers;

/// <summary>Fires every so many periods of 50 milliseconds (1 to 10, from the dialog's slider).</summary>
[WiredBox("wf_trg_period_short")]
public sealed class RepeatShortTrigger : WiredTrigger
{
    public const int MinTicks = 1;
    public const int MaxTicks = 10;

    public override int Code => TriggerCodes.PeriodicShort;
    public override IReadOnlyList<int> DefaultIntParams => [4];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Matches(WiredBox box, WiredEvent e, IWiredRoom room) =>
        e.Type == WiredEventType.ShortTick
        && RepeatTrigger.CountDown(box, Math.Clamp(box.IntParam(0, MinTicks), MinTicks, MaxTicks));
}
