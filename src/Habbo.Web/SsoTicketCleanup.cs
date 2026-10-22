using Habbo.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Habbo.Web;

/// <summary>Deletes the SSO tickets the website handed out once they have expired (used or not), every so often, so
/// the table holds only the ones still good. A permanent ticket is kept.</summary>
public sealed class SsoTicketCleanup(IDbContextFactory<AppDbContext> dbFactory, ILogger<SsoTicketCleanup> logger) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Every);
        do
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync(stoppingToken);
                var now = DateTimeOffset.UtcNow;
                var deleted = await db.SsoTickets.Where(t => !t.Permanent && t.ExpiresAt < now).ExecuteDeleteAsync(stoppingToken);
                if (deleted > 0)
                    logger.LogInformation("Deleted {Count} expired SSO tickets.", deleted);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // tried again next time
                logger.LogWarning(e, "Deleting expired SSO tickets failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
