using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Domain.Moderation;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>ModKickMessageEvent — send a user out of their room.</summary>
internal sealed class ModKickHandler(IModerationService service) : IMessageHandler<ModKickMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ModKickMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SanctionAsync(player.Id, new ModerationAction(SanctionKind.Kick, message.UserId, message.Message, message.TopicId, message.IssueId));
        return true;
    }
}
