namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when the time of day (in the box's time zone) is within the chosen seconds,
/// minutes and hours — each filter exact, a range, or not used.</summary>
[WiredBox("wf_cnd_match_time")]
public sealed class TimeMatchesCondition : WiredCondition
{
    public override int Code => ConditionCodes.TimeMatches;

    // uses: seconds, minutes, hours; then min and max of each
    public override IReadOnlyList<int> DefaultIntParams => [0, 0, 0, 0, 0, 0, 0, 0, 0];

    public override bool Passes(WiredBox box, WiredRun run)
    {
        var now = WiredTime.Now(box, run.Room);
        return WiredTime.InRange(box, 0, 3, now.Second)
               && WiredTime.InRange(box, 1, 5, now.Minute)
               && WiredTime.InRange(box, 2, 7, now.Hour);
    }
}
