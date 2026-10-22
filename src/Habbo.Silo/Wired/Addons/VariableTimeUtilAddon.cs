using System.Globalization;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons;

/// <summary>In a variable box's stack, reads a time — the variable's value as seconds since 1970,
/// or when it was given, or last changed — and gives the variable sub-variables for its parts (the
/// hour, the day of the week, the year) and for how many of each unit have passed since 1970, in
/// the room's time zone.</summary>
[WiredBox("wf_xtra_var_time_util")]
public sealed class VariableTimeUtilAddon : WiredAddon
{
    private const int SubVariableMask = 0;
    private const int Mode = 1;

    // time_util.mode.N
    private const int FromValue = 0;
    private const int CreationTime = 1;

    private const int DaysPerWeek = 7;
    private const int MonthsPerYear = 12;
    private const int FirstYear = 1970;

    public override int Code => AddonCodes.VariableTimeUtil;
    public override IReadOnlyList<int> DefaultIntParams => [0, CreationTime];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    private static readonly (int Bit, string Name, Func<DateTimeOffset, long> Read)[] Offered =
    [
        (1, "milliseconds_of_seconds", t => t.Millisecond),
        (2, "seconds_of_minute", t => t.Second),
        (3, "minute_of_hour", t => t.Minute),
        (4, "hour_of_day", t => t.Hour),
        (5, "day_of_week", t => ((int)t.DayOfWeek + DaysPerWeek - 1) % DaysPerWeek + 1),
        (6, "day_of_month", t => t.Day),
        (7, "day_of_year", t => t.DayOfYear),
        (8, "week_of_year", t => ISOWeek.GetWeekOfYear(t.DateTime)),
        (9, "month_of_year", t => t.Month),
        (10, "year", t => t.Year),
        (20, "millisecond", t => t.ToUnixTimeMilliseconds()),
        (21, "second", t => t.ToUnixTimeSeconds()),
        (22, "minute", t => t.ToUnixTimeSeconds() / 60),
        (23, "hour", t => t.ToUnixTimeSeconds() / 3600),
        (24, "day", t => t.ToUnixTimeSeconds() / 86400),
        (25, "week", t => t.ToUnixTimeSeconds() / (86400 * DaysPerWeek)),
        (26, "month", t => (t.Year - FirstYear) * MonthsPerYear + t.Month - 1),
    ];

    public static IEnumerable<WiredVariable> SubVariables(WiredBox addon, WiredVariable parent, IWiredRoom room)
    {
        var mode = addon.IntParam(Mode);
        foreach (var (bit, name, read) in Offered.Where(o => (addon.IntParam(SubVariableMask) & (1 << o.Bit)) != 0))
        {
            yield return WiredSubVariables.Of(parent, name, (value, times) =>
            {
                DateTimeOffset? time = mode switch
                {
                    FromValue => value is { } seconds ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null,
                    CreationTime => times?.Created,
                    _ => times?.Updated,
                };
                return time is { } t ? (int)Math.Clamp(read(Local(t, room.Timezone)), int.MinValue, int.MaxValue) : null;
            });
        }
    }

    private static DateTimeOffset Local(DateTimeOffset time, string zone) =>
        zone.Length > 0 && TimeZoneInfo.TryFindSystemTimeZoneById(zone, out var timeZone) ? TimeZoneInfo.ConvertTime(time, timeZone) : time;
}
