using Habbo.Protocol.Messages.Incoming.Tracking;
using Habbo.Silo.Abstractions.Hotel;

namespace Habbo.Gateway.Messages.Handlers.Tracking;

/// <summary>LatencyPingReportMessageEvent — the client's latency.</summary>
internal sealed class LatencyPingReportHandler(IClientTrackingService service) : IMessageHandler<LatencyPingReportMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, LatencyPingReportMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.LatencyAsync(player, message.Average, message.ValidAverage, message.Count);
        return true;
    }
}
