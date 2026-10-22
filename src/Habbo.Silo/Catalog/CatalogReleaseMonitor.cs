using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Habbo.Silo.Catalog;

/// <summary>Publishes the catalogue when an offer set for a time goes on sale, a rare's release among them: everyone
/// online is told, and their client drops the catalogue it has for the new one.</summary>
public sealed class CatalogReleaseMonitor(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    PlayerMessenger messenger,
    ILogger<CatalogReleaseMonitor> logger) : BackgroundService
{
    /// <summary>How often releases are looked for; a release is published within this much of its time.</summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    /// <summary>The client refreshes a catalogue that is open at once.</summary>
    private const bool InstantlyRefresh = true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var checkedUntil = DateTimeOffset.UtcNow;
        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = DateTimeOffset.UtcNow;
            try
            {
                await PublishReleasesAsync(checkedUntil, now, stoppingToken);
                checkedUntil = now;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not look for catalogue releases");
            }
        }
    }

    private async Task PublishReleasesAsync(DateTimeOffset after, DateTimeOffset upTo, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var released = await db.CatalogOffers.AsNoTracking()
            .Where(o => o.AvailableFrom > after && o.AvailableFrom <= upTo)
            .Select(o => o.Id)
            .ToListAsync(ct);
        if (released.Count == 0)
            return;

        var online = await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).GetOnlineAsync();
        logger.LogInformation("Catalogue published: offer(s) {Offers} went on sale; telling {Online} online", released, online.Count);
        await messenger.SendAsync(online, new CatalogPublishedMessageComposer(InstantlyRefresh));
    }
}
