using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GuideSessionResolvedMessageEvent — the guide is done.</summary>
internal sealed class GuideSessionResolvedHandler(IGrainFactory service) : IMessageHandler<GuideSessionResolvedMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GuideSessionResolvedMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).ResolveAsync(player);
        return true;
    }
}
