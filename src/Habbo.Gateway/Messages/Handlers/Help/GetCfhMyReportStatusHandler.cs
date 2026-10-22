using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GetCfhMyReportStatusEvent — how the player's reports went.</summary>
internal sealed class GetCfhMyReportStatusHandler(IModerationService service) : IMessageHandler<GetCfhMyReportStatusEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetCfhMyReportStatusEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendMyReportStatusAsync(player.Id);
        return true;
    }
}
