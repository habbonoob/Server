using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GuideSessionFeedbackMessageEvent — rate the guide.</summary>
internal sealed class GuideSessionFeedbackHandler(IGrainFactory service) : IMessageHandler<GuideSessionFeedbackMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GuideSessionFeedbackMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).FeedbackAsync(player, message.Recommend);
        return true;
    }
}
