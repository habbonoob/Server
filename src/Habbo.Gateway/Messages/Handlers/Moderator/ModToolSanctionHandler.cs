using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>ModToolSanctionEvent — what a default sanction would be.</summary>
internal sealed class ModToolSanctionHandler(IModerationService service) : IMessageHandler<ModToolSanctionEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ModToolSanctionEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendSanctionPreviewAsync(player.Id, message.IssueId, message.AccountId, message.TopicId);
        return true;
    }
}
