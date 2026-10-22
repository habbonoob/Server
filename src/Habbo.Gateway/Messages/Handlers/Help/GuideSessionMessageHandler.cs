using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GuideSessionMessageMessageEvent — say something in the session.</summary>
internal sealed class GuideSessionMessageHandler(IGrainFactory service) : IMessageHandler<GuideSessionMessageMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GuideSessionMessageMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).SayAsync(player, message.Message);
        return true;
    }
}
