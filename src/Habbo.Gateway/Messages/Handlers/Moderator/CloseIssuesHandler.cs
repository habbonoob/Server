using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>CloseIssuesMessageEvent — close issues with a verdict.</summary>
internal sealed class CloseIssuesHandler(IModerationService service) : IMessageHandler<CloseIssuesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CloseIssuesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.CloseIssuesAsync(player.Id, message.IssueIds, message.CloseReason);
        return true;
    }
}
