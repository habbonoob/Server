namespace Habbo.Domain.LandingView;

/// <summary>One code and the window it shows in.</summary>
public sealed class TimingScheduleEntry
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }
    public TimingSchedule? Schedule { get; set; }

    /// <summary>What the client draws — it picks its graphics by this string.</summary>
    public required string Code { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    /// <summary>Which entry wins when windows overlap; the lowest comes first.</summary>
    public int Priority { get; set; }

    public bool CoversNow(DateTimeOffset now) => StartsAt <= now && now < EndsAt;
}
