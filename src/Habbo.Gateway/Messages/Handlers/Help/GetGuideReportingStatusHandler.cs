using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GetGuideReportingStatusMessageEvent — whether the player may ask now.</summary>
internal sealed class GetGuideReportingStatusHandler(IGrainFactory service) : IMessageHandler<GetGuideReportingStatusMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetGuideReportingStatusMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).SendReportingStatusAsync(player);
        return true;
    }
}
