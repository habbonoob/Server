using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>ChatReviewSessionCreateMessageEvent — report bullying.</summary>
internal sealed class ChatReviewSessionCreateHandler(IGrainFactory service) : IMessageHandler<ChatReviewSessionCreateMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ChatReviewSessionCreateMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.GetGrain<IGuideDeskGrain>(0).CreateChatReviewAsync(player, message.ReportedUserId, message.RoomId);
        return true;
    }
}
