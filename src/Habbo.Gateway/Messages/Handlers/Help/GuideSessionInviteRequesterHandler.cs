using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GuideSessionInviteRequesterMessageEvent — invite the requester to the guide's room.</summary>
internal sealed class GuideSessionInviteRequesterHandler(IGrainFactory service) : IMessageHandler<GuideSessionInviteRequesterMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GuideSessionInviteRequesterMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).InviteRequesterAsync(player);
        return true;
    }
}
