namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes between the box's start and end dates (seconds since 1970); without an end, any
/// time after the start; without dates, always.</summary>
[WiredBox("wf_cnd_date_rng_active")]
public sealed class DateRangeCondition : WiredCondition
{
    public override int Code => ConditionCodes.DateRangeActive;

    public override bool Passes(WiredBox box, WiredRun run)
    {
        var now = run.Room.Now().ToUnixTimeSeconds();
        var settings = box.IntParams;
        return (settings.Count < 1 || now >= settings[0]) && (settings.Count < 2 || now <= settings[1]);
    }
}
