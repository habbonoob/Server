using Habbo.Protocol.Messages.Incoming.Moderator;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Moderator;

/// <summary>PickIssuesMessageEvent — take issues on.</summary>
internal sealed class PickIssuesHandler(IModerationService service) : IMessageHandler<PickIssuesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PickIssuesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.PickIssuesAsync(player.Id, message.IssueIds, message.RetryEnabled, message.RetryCount, message.Message);
        return true;
    }
}
