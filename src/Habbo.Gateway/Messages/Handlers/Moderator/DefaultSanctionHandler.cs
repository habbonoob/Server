using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>DefaultSanctionMessageEvent — the next step of the sanction ladder for a user.</summary>
internal sealed class DefaultSanctionHandler(IModerationService service) : IMessageHandler<DefaultSanctionMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, DefaultSanctionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.DefaultSanctionAsync(player.Id, message.UserId, message.TopicId, message.Message, message.IssueId);
        return true;
    }
}
