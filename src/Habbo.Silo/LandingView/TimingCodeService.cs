using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Hotel;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.LandingView;

/// <summary>Picks the timing code a landing-view schedule is showing. Windows may overlap —
/// a seasonal run inside a year-long one — so the lowest priority wins, then the latest start.
/// </summary>
public sealed class TimingCodeService(IDbContextFactory<AppDbContext> dbFactory) : ITimingCodeService
{
    public async Task<string> GetCurrentCodeAsync(string schedulingKey, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(schedulingKey))
            return string.Empty;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = DateTimeOffset.UtcNow;

        var entry = await db.TimingScheduleEntries
            .AsNoTracking()
            .Where(row => row.Schedule!.SchedulingKey == schedulingKey
                          && row.StartsAt <= now && now < row.EndsAt)
            .OrderBy(row => row.Priority)
            .ThenByDescending(row => row.StartsAt)
            .FirstOrDefaultAsync(ct);

        return entry?.Code ?? string.Empty;
    }
}
