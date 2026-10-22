using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>GetCfhChatlogMessageEvent — an issue's chat log.</summary>
internal sealed class GetCfhChatlogHandler(IModerationService service) : IMessageHandler<GetCfhChatlogMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetCfhChatlogMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendIssueChatlogAsync(player.Id, message.IssueId);
        return true;
    }
}
