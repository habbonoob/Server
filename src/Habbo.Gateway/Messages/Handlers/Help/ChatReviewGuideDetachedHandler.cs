using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>ChatReviewGuideDetachedMessageEvent — leave a review.</summary>
internal sealed class ChatReviewGuideDetachedHandler(IGrainFactory service) : IMessageHandler<ChatReviewGuideDetachedMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ChatReviewGuideDetachedMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).LeaveReviewAsync(player);
        return true;
    }
}
