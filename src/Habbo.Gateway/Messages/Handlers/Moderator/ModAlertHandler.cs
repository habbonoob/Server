using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Domain.Moderation;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>ModAlertMessageEvent — caution a user.</summary>
internal sealed class ModAlertHandler(IModerationService service) : IMessageHandler<ModAlertMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ModAlertMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SanctionAsync(player.Id, new ModerationAction(SanctionKind.Alert, message.UserId, message.Message, message.TopicId, message.IssueId));
        return true;
    }
}
