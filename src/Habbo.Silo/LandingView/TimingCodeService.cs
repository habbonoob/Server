using System.Globalization;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Hotel;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.LandingView;

/// <summary>Picks the timing code a landing-view schedule is showing. The client sends the schedule itself, as its
/// external variables write it ("2026-10-21 11:00,hween26clothcrack;2026-10-22 11:00,apr26offer1"), and Habbo answers
/// with the code of the latest entry that has begun (habbo.com, 25 October 2026: that one, "apr26offer1"). A key
/// that is no such list names a schedule of the hotel's own: windows there may overlap — a seasonal run inside a
/// year-long one — so the lowest priority wins, then the latest start.</summary>
public sealed class TimingCodeService(IDbContextFactory<AppDbContext> dbFactory) : ITimingCodeService
{
    private const string StartFormat = "yyyy-MM-dd HH:mm";

    public async Task<string> GetCurrentCodeAsync(string schedulingKey, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(schedulingKey))
            return string.Empty;

        var now = DateTimeOffset.UtcNow;
        if (ParseSchedule(schedulingKey) is { } schedule)
            return CurrentOf(schedule, now);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entry = await db.TimingScheduleEntries
            .AsNoTracking()
            .Where(row => row.Schedule!.SchedulingKey == schedulingKey
                          && row.StartsAt <= now && now < row.EndsAt)
            .OrderBy(row => row.Priority)
            .ThenByDescending(row => row.StartsAt)
            .FirstOrDefaultAsync(ct);

        return entry?.Code ?? string.Empty;
    }

    /// <summary>The code of the latest entry begun by now; empty before the first.</summary>
    public static string CurrentOf(IReadOnlyList<(DateTimeOffset Start, string Code)> schedule, DateTimeOffset now) =>
        schedule.Where(entry => entry.Start <= now).OrderBy(entry => entry.Start).LastOrDefault().Code ?? string.Empty;

    /// <summary>The entries of a schedule the client sent, its times taken as UTC; null when the key is not one.</summary>
    public static IReadOnlyList<(DateTimeOffset Start, string Code)>? ParseSchedule(string key)
    {
        var entries = new List<(DateTimeOffset, string)>();
        foreach (var part in key.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var comma = part.IndexOf(',');
            if (comma < 0 || !DateTime.TryParseExact(part[..comma].Trim(), StartFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var start))
                return null;
            entries.Add((new DateTimeOffset(start, TimeSpan.Zero), part[(comma + 1)..].Trim()));
        }

        return entries.Count == 0 ? null : entries;
    }
}
