using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Domain.Moderation;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>ModMessageMessageEvent — message a user.</summary>
internal sealed class ModMessageHandler(IModerationService service) : IMessageHandler<ModMessageMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ModMessageMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SanctionAsync(player.Id, new ModerationAction(SanctionKind.Message, message.UserId, message.Message, message.TopicId, message.IssueId));
        return true;
    }
}
