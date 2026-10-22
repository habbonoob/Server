using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>ChatReviewGuideVoteMessageEvent — a guardian's verdict.</summary>
internal sealed class ChatReviewGuideVoteHandler(IGrainFactory service) : IMessageHandler<ChatReviewGuideVoteMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ChatReviewGuideVoteMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).VoteAsync(player, message.Vote);
        return true;
    }
}
