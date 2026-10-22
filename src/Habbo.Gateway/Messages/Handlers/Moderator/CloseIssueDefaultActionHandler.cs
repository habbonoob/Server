using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>CloseIssueDefaultActionMessageEvent — close a bundle with the default sanction.</summary>
internal sealed class CloseIssueDefaultActionHandler(IModerationService service) : IMessageHandler<CloseIssueDefaultActionMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CloseIssueDefaultActionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.CloseWithDefaultSanctionAsync(player.Id, message.IssueId, message.OtherIssueIds, message.TopicId);
        return true;
    }
}
