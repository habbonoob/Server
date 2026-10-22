using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>ReleaseIssuesMessageEvent — put picked issues back.</summary>
internal sealed class ReleaseIssuesHandler(IModerationService service) : IMessageHandler<ReleaseIssuesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ReleaseIssuesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ReleaseIssuesAsync(player.Id, message.IssueIds);
        return true;
    }
}
