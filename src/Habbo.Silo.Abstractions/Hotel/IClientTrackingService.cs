using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>What clients report about how they run, kept in the hotel's log.</summary>
public interface IClientTrackingService
{
    Task LatencyAsync(PlayerData player, int average, int validAverage, int count);

    Task PerformanceAsync(PlayerData player, ClientPerformance performance);

    Task LagWarningAsync(PlayerData player, int warningCount);
}
