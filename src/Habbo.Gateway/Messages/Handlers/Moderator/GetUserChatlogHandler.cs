using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>GetUserChatlogMessageEvent — what a user said lately.</summary>
internal sealed class GetUserChatlogHandler(IModerationService service) : IMessageHandler<GetUserChatlogMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetUserChatlogMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendUserChatlogAsync(player.Id, message.UserId);
        return true;
    }
}
