namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when the date (in the box's time zone) is on one of the chosen weekdays and
/// months, and within the chosen days of the month and years.</summary>
[WiredBox("wf_cnd_match_date")]
public sealed class DateMatchesCondition : WiredCondition
{
    private const int WeekdayMask = 2;
    private const int MonthMask = 5;

    public override int Code => ConditionCodes.DateMatches;

    // uses: day, year; weekday mask; day min, max; month mask; year min, max
    public override IReadOnlyList<int> DefaultIntParams => [0, 0, 127, 1, 1, 4095, 0, 0];

    public override bool Passes(WiredBox box, WiredRun run)
    {
        var now = WiredTime.Now(box, run.Room);
        // Monday is the first weekday box, January the first month box
        var weekday = ((int)now.DayOfWeek + 6) % 7;
        return WiredTime.InMask(box.IntParam(WeekdayMask), weekday)
               && WiredTime.InMask(box.IntParam(MonthMask), now.Month - 1)
               && WiredTime.InRange(box, 0, 3, now.Day)
               && WiredTime.InRange(box, 1, 6, now.Year);
    }
}
