namespace Habbo.Silo.Wired.Conditions;

/// <summary>"Active before": passes while the room's wired timer has not yet run the box's time.</summary>
[WiredBox("wf_cnd_time_less_than")]
public sealed class TimeElapsedLessCondition : WiredCondition
{
    public override int Code => ConditionCodes.TimeElapsedLess;
    public override IReadOnlyList<int> DefaultIntParams => [20];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Passes(WiredBox box, WiredRun run) => run.Room.TimerPulses < TimeElapsedMoreCondition.Pulses(box);
}
