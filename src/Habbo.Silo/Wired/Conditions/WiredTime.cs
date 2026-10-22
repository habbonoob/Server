namespace Habbo.Silo.Wired.Conditions;

/// <summary>The time and date conditions' shared reading of the clock and their filters.</summary>
public static class WiredTime
{
    /// <summary>The room's time in the box's time zone (its string parameter, as the client's
    /// wired.timezones lists them), else the room's; UTC when neither names one the server knows.</summary>
    public static DateTimeOffset Now(WiredBox box, IWiredRoom room)
    {
        var now = room.Now();
        var zone = string.IsNullOrEmpty(box.Settings.StringParam) ? room.Timezone : box.Settings.StringParam;
        return !string.IsNullOrEmpty(zone) && TimeZoneInfo.TryFindSystemTimeZoneById(zone, out var timeZone)
            ? TimeZoneInfo.ConvertTime(now, timeZone)
            : now.ToUniversalTime();
    }

    /// <summary>Whether a filter passes the value: not used, or the value within its min and max —
    /// a range that wraps (22 to 2 o'clock) wrapping.</summary>
    public static bool InRange(WiredBox box, int useIndex, int minIndex, int value)
    {
        if (!box.Flag(useIndex))
            return true;

        var (min, max) = (box.IntParam(minIndex), box.IntParam(minIndex + 1));
        return min <= max ? value >= min && value <= max : value >= min || value <= max;
    }

    /// <summary>Whether a checkbox mask allows the value's bit; no boxes ticked filters nothing.</summary>
    public static bool InMask(int mask, int bit) => mask == 0 || (mask & (1 << bit)) != 0;
}
