namespace Habbo.Silo.Wired.Conditions;

/// <summary>"Active after": passes once the room's wired timer has run the box's time. The dialog
/// stores its slider (pulses of half a second, 1 to 1200) plus one.</summary>
[WiredBox("wf_cnd_time_more_than")]
public sealed class TimeElapsedMoreCondition : WiredCondition
{
    public override int Code => ConditionCodes.TimeElapsedMore;
    public override IReadOnlyList<int> DefaultIntParams => [20];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Passes(WiredBox box, WiredRun run) => run.Room.TimerPulses > Pulses(box);

    /// <summary>The time the slider shows, in pulses.</summary>
    public static int Pulses(WiredBox box) => box.IntParam(0, 2) - 1;
}
