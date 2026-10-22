namespace Habbo.Silo.Wired.Addons;

/// <summary>Lets the stack run at most so many times (1 to 100) within a time window (1 to 20
/// pulses); runs beyond that are dropped.</summary>
[WiredBox("wf_xtra_execution_limit")]
public sealed class ExecutionLimitAddon : WiredAddon
{
    private const int Executions = 0;
    private const int WindowPulses = 1;

    public override int Code => AddonCodes.ExceedExecutions;

    /// <summary>A new box on Habbo: [100, 1].</summary>
    public override IReadOnlyList<int> DefaultIntParams => [100, 1];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    /// <summary>Whether the stack may run now, counting the run if so.</summary>
    public static bool TakeRun(WiredBox box, int pulse)
    {
        var window = Math.Clamp(box.IntParam(WindowPulses, 1), 1, 20);
        box.RecentRuns.RemoveAll(run => run <= pulse - window);
        if (box.RecentRuns.Count >= Math.Clamp(box.IntParam(Executions, 1), 1, 100))
            return false;

        box.RecentRuns.Add(pulse);
        return true;
    }
}
