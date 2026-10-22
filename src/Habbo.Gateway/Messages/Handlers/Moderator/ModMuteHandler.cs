using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Domain.Moderation;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>ModMuteMessageEvent — mute a user.</summary>
internal sealed class ModMuteHandler(IModerationService service) : IMessageHandler<ModMuteMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ModMuteMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SanctionAsync(player.Id, new ModerationAction(SanctionKind.Mute, message.UserId, message.Message, message.TopicId, message.IssueId));
        return true;
    }
}
