using Habbo.Silo.Abstractions.Permissions;
using Habbo.Domain.Campaigns;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Campaign;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;
using Habbo.Silo.Permissions;
using Habbo.Silo.Users;

namespace Habbo.Silo.Campaigns;

/// <summary>Campaign calendars (seed/campaign_calendars.json): a door a day, each opened once for the furniture
/// behind it. Today's door opens, and for a few days after; a door left longer is missed. Staff may open any.</summary>
public sealed class CampaignCalendarService(
    IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains, PlayerMessenger messenger, PermissionService permissions,
    IOptions<CampaignCalendarOptions> options)
    : ICampaignCalendarService
{
    public async Task SendAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await RunningAsync(db) is not { } running)
            return;

        var (calendar, today) = running;
        var opened = await OpenedAsync(db, player.Id, calendar.Name);
        var missed = Enumerable.Range(0, Math.Max(0, today - options.Value.CatchUpDays)).Except(opened).ToList();
        await messenger.SendAsync(player.Id, new CampaignCalendarDataMessageComposer(
            calendar.Name, calendar.Image, today, calendar.Doors.Count, opened, missed));
    }

    public async Task OpenDoorAsync(PlayerData player, string campaignName, int day, bool asStaff)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var door = await RunningAsync(db) is { } running && running.Calendar.Name == campaignName && MayOpen(player, day, running.Today, asStaff)
            ? running.Calendar.Doors.FirstOrDefault(d => d.Day == day)
            : null;
        var definitionId = door is null ? null
            : await db.ItemDefinitions.Where(d => d.ClassName == door.FurniClassName).Select(d => (int?)d.Id).FirstOrDefaultAsync();
        if (door is null || definitionId is null || !await RecordOpeningAsync(db, player.Id, campaignName, day))
        {
            await messenger.SendAsync(player.Id, new CampaignCalendarDoorOpenedMessageComposer(false, string.Empty, string.Empty, string.Empty));
            return;
        }

        await grains.GetGrain<IPlayerGrain>(player.Id).ReceiveFurnitureAsync(definitionId.Value, string.Empty, 1);
        await messenger.SendAsync(player.Id, new CampaignCalendarDoorOpenedMessageComposer(true, door.ProductCode, door.CustomImage, door.FurniClassName));
    }

    /// <summary>Today's door and those still in their catch-up days; staff any door of the calendar.</summary>
    private bool MayOpen(PlayerData player, int day, int today, bool asStaff) =>
        asStaff ? permissions.Has(player, PermissionNames.CalendarOpenAnyDay) : day <= today && day >= today - options.Value.CatchUpDays;

    /// <summary>The calendar running today, with which of its days today is. When calendars overlap the latest started wins.</summary>
    private static async Task<(CampaignCalendar Calendar, int Today)?> RunningAsync(AppDbContext db)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var calendars = await db.CampaignCalendars.AsNoTracking().Include(c => c.Doors).Where(c => c.StartsOn <= today)
            .OrderByDescending(c => c.StartsOn).ToListAsync();
        var running = calendars.FirstOrDefault(c => today.DayNumber - c.StartsOn.DayNumber < c.Doors.Count);
        return running is null ? null : (running, today.DayNumber - running.StartsOn.DayNumber);
    }

    private static Task<List<int>> OpenedAsync(AppDbContext db, Guid playerId, string campaignName) =>
        db.CampaignCalendarOpenings.Where(o => o.UserId == playerId && o.CampaignName == campaignName).OrderBy(o => o.Day).Select(o => o.Day).ToListAsync();

    /// <summary>Records the door as opened; false when it already was, so a door never gives twice.</summary>
    private static async Task<bool> RecordOpeningAsync(AppDbContext db, Guid playerId, string campaignName, int day)
    {
        db.CampaignCalendarOpenings.Add(new CampaignCalendarOpening { UserId = playerId, CampaignName = campaignName, Day = day });
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}
