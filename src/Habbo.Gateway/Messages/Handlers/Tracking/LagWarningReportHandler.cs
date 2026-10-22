using Habbo.Protocol.Messages.Incoming.Tracking;
using Habbo.Silo.Abstractions.Hotel;

namespace Habbo.Gateway.Messages.Handlers.Tracking;

/// <summary>LagWarningReportMessageEvent — the client lagged.</summary>
internal sealed class LagWarningReportHandler(IClientTrackingService service) : IMessageHandler<LagWarningReportMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, LagWarningReportMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.LagWarningAsync(player, message.WarningCount);
        return true;
    }
}
