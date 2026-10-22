using Habbo.Domain.Campaigns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The campaigns' calendars.</summary>
public static partial class DbSeeder
{
    private sealed record SeedCampaignCalendars(List<SeedCampaignCalendar>? Calendars);

    private sealed record SeedCampaignCalendar(string Name, string Image, DateOnly StartsOn, List<SeedCampaignCalendarDoor>? Doors);

    /// <summary>A door; the product it names is the furniture's own unless it says otherwise.</summary>
    private sealed record SeedCampaignCalendarDoor(string Furni, string? ProductCode, string? CustomImage);

    /// <summary>seed/campaign_calendars.json, a door a day in the order listed. A calendar the hotel already has
    /// keeps whatever it was changed to.</summary>
    private static async Task SeedCampaignCalendarsAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var file = await seedFiles.ReadJsonAsync<SeedCampaignCalendars>("campaign_calendars.json", ct);
        var known = await db.CampaignCalendars.Select(c => c.Name).ToListAsync(ct);
        var added = 0;
        foreach (var seed in (file?.Calendars ?? []).Where(c => !known.Contains(c.Name)))
        {
            var day = 0;
            db.CampaignCalendars.Add(new CampaignCalendar
            {
                Name = seed.Name, Image = seed.Image, StartsOn = seed.StartsOn,
                Doors = (seed.Doors ?? []).Select(d => new CampaignCalendarDoor
                {
                    CampaignName = seed.Name, Day = day++, FurniClassName = d.Furni, ProductCode = d.ProductCode ?? d.Furni,
                    CustomImage = d.CustomImage ?? string.Empty,
                }).ToList(),
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} campaign calendar(s) from {File}.", added, "campaign_calendars.json");
        }
    }
}
