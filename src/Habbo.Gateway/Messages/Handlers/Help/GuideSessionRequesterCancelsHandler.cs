using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GuideSessionRequesterCancelsMessageEvent — the requester gives up.</summary>
internal sealed class GuideSessionRequesterCancelsHandler(IGrainFactory service) : IMessageHandler<GuideSessionRequesterCancelsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GuideSessionRequesterCancelsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).RequesterCancelsAsync(player);
        return true;
    }
}
