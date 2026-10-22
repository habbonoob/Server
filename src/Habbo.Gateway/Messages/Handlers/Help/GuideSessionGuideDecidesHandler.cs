using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GuideSessionGuideDecidesMessageEvent — take or pass on a request.</summary>
internal sealed class GuideSessionGuideDecidesHandler(IGrainFactory service) : IMessageHandler<GuideSessionGuideDecidesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GuideSessionGuideDecidesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).GuideDecidesAsync(player, message.Accept);
        return true;
    }
}
