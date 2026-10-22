using Habbo.Protocol.Messages.Incoming.Tracking;
using Habbo.Silo.Abstractions.Hotel;

namespace Habbo.Gateway.Messages.Handlers.Tracking;

/// <summary>PerformanceLogMessageEvent — how the client performs.</summary>
internal sealed class PerformanceLogHandler(IClientTrackingService service) : IMessageHandler<PerformanceLogMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PerformanceLogMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.PerformanceAsync(player, new ClientPerformance(message.ElapsedSeconds, message.UserAgent, message.FlashVersion, message.OperatingSystem, message.Browser, message.IsDebugger, message.MemoryKb, message.GarbageCollections, message.SlowUpdates, message.AverageUpdateInterval, message.LagSpikes));
        return true;
    }
}
