using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>ChatReviewGuideDecidesOnOfferMessageEvent — take or pass on a review.</summary>
internal sealed class ChatReviewGuideDecidesOnOfferHandler(IGrainFactory service) : IMessageHandler<ChatReviewGuideDecidesOnOfferMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ChatReviewGuideDecidesOnOfferMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).ReviewDecidesAsync(player, message.Accept);
        return true;
    }
}
