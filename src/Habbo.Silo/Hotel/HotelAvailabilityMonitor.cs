using Habbo.Silo.Abstractions.Permissions;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Availability;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Habbo.Silo.Permissions;
using Habbo.Silo.Users;

namespace Habbo.Silo.Hotel;

/// <summary>Keeps the hotel to its hours: warns everyone as closing or maintenance draws near, and when the
/// hotel closes tells those who may not stay and, when closing sends them away, disconnects them.</summary>
public sealed class HotelAvailabilityMonitor(
    IHotelAvailabilityService availability,
    IOptionsMonitor<HotelAvailabilityOptions> options,
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    PlayerMessenger messenger,
    PermissionService permissions,
    ILogger<HotelAvailabilityMonitor> logger) : BackgroundService
{
    /// <summary>How often the hours are checked; well inside a minute, so no warning is missed.</summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);

    private bool? _wasOpen;
    private int? _closingWarned;
    private int? _maintenanceWarned;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not check the hotel's opening hours");
            }
        }
    }

    private async Task CheckAsync()
    {
        var now = availability.GetAvailability();
        var warnings = options.CurrentValue.WarningMinutes;

        _closingWarned = await WarnAsync(now.MinutesUntilClosing, _closingWarned, warnings,
            minutes => new InfoHotelClosingMessageComposer(minutes));
        _maintenanceWarned = await WarnAsync(now.MinutesUntilMaintenance, _maintenanceWarned, warnings,
            minutes => new MaintenanceStatusMessageComposer(false, minutes, now.MaintenanceMinutes));

        if (_wasOpen == true && !now.IsOpen)
            await CloseAsync(now);
        _wasOpen = now.IsOpen;
    }

    /// <summary>Warns everyone once as each warning's minute comes round; returns the minute last warned of.</summary>
    private async Task<int?> WarnAsync(int? minutesLeft, int? warned, List<int> warnings, Func<int, IComposer> warning)
    {
        if (minutesLeft is not { } minutes || !warnings.Contains(minutes))
            return null;
        if (warned == minutes)
            return warned;

        await messenger.SendAsync(await OnlineAsync(), warning(minutes));
        return minutes;
    }

    /// <summary>Maintenance always sends everyone away; closing time only when the hotel is set to.</summary>
    private async Task CloseAsync(HotelAvailability now)
    {
        var throwOut = now.InMaintenance || options.CurrentValue.ThrowOutAtClose;
        var online = await OnlineAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var leaving = (await db.Users.AsNoTracking().Where(u => online.Contains(u.Id)).Select(u => new { u.Id, u.Rank }).ToListAsync())
            .Where(u => !permissions.Has(u.Id, u.Rank, PermissionNames.HotelEnterWhileClosed)).Select(u => u.Id).ToList();

        await messenger.SendAsync(leaving, new InfoHotelClosedMessageComposer(now.ReopenHour, now.ReopenMinute, throwOut));
        if (throwOut)
        {
            var reason = now.InMaintenance
                ? Habbo.Protocol.Messages.Outgoing.Handshake.DisconnectReasonComposer.MaintenanceBreak
                : Habbo.Protocol.Messages.Outgoing.Handshake.DisconnectReasonComposer.HotelClosed;
            await Task.WhenAll(leaving.Select(id => grains.GetGrain<IPlayerGrain>(id).DisconnectAsync(reason)));
        }
        logger.LogInformation("The hotel closed until {Hour:00}:{Minute:00}; {Count} player(s) {Fate}",
            now.ReopenHour, now.ReopenMinute, leaving.Count, throwOut ? "sent away" : "told");
    }

    private Task<List<Guid>> OnlineAsync() => grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).GetOnlineAsync();
}
