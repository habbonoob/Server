using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GuideSessionCreateMessageEvent — ask for a guide.</summary>
internal sealed class GuideSessionCreateHandler(IGrainFactory service) : IMessageHandler<GuideSessionCreateMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GuideSessionCreateMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).CreateRequestAsync(player, message.RequestType, message.Description);
        return true;
    }
}
