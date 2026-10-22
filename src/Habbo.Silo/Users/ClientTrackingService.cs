using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Collections;
using Microsoft.Extensions.Logging;

namespace Habbo.Silo.Users;

/// <summary>Keeps what clients report about how they run in the hotel's log, per player.</summary>
public sealed class ClientTrackingService(ILogger<ClientTrackingService> logger) : IClientTrackingService
{
    /// <summary>One report of each kind a player a minute reaches the log: the client sends them on its own timers,
    /// but a modified one could send them as fast as it likes, and fill the disk.</summary>
    private readonly RecentlySeen<(Guid Player, string Kind)> _logged = new(TimeSpan.FromMinutes(1));

    public Task LatencyAsync(PlayerData player, int average, int validAverage, int count)
    {
        if (_logged.FirstInWindow((player.Id, nameof(LatencyAsync))))
            logger.LogInformation("Latency of {Player}: {Average} ms average, {ValidAverage} ms without outliers, over {Count} pings",
                player.Username, average, validAverage, count);
        return Task.CompletedTask;
    }

    public Task PerformanceAsync(PlayerData player, ClientPerformance performance)
    {
        if (_logged.FirstInWindow((player.Id, nameof(PerformanceAsync))))
            logger.LogInformation("Performance of {Player}: {Performance}", player.Username, performance);
        return Task.CompletedTask;
    }

    public Task LagWarningAsync(PlayerData player, int warningCount)
    {
        if (_logged.FirstInWindow((player.Id, nameof(LagWarningAsync))))
            logger.LogWarning("{Player}'s client lagged {Count} time(s)", player.Username, warningCount);
        return Task.CompletedTask;
    }
}
