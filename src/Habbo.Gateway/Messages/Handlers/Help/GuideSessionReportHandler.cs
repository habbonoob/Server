using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GuideSessionReportMessageEvent — report the guide.</summary>
internal sealed class GuideSessionReportHandler(IGrainFactory service) : IMessageHandler<GuideSessionReportMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GuideSessionReportMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).ReportGuideAsync(player, message.Message);
        return true;
    }
}
