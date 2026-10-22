using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GuideSessionGetRequesterRoomMessageEvent — go to the requester's room.</summary>
internal sealed class GuideSessionGetRequesterRoomHandler(IGrainFactory service) : IMessageHandler<GuideSessionGetRequesterRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GuideSessionGetRequesterRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).GoToRequesterRoomAsync(player);
        return true;
    }
}
