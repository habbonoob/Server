using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Domain.Moderation;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>ModBanMessageEvent — ban a user.</summary>
internal sealed class ModBanHandler(IModerationService service) : IMessageHandler<ModBanMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ModBanMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SanctionAsync(player.Id, new ModerationAction(SanctionKind.Ban, message.UserId, message.Message, message.TopicId, message.IssueId, message.SanctionTypeId, AvatarOnly: message.AvatarOnly));
        return true;
    }
}
