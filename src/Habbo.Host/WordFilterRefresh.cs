using Habbo.Silo.Chat;
using Microsoft.Extensions.Hosting;

namespace Habbo.Host;

/// <summary>The website's API on its own reads the word filter (which names it turns away) again every minute: :update
/// reaches the hotel's processes, not this one.</summary>
public sealed class WordFilterRefresh(WordFilter wordFilter) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Every);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await wordFilter.ReloadAsync();
    }
}
